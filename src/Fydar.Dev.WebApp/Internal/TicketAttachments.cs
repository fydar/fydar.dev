using Amazon.S3;
using Fydar.Dev.Services.EmailTickets;
using Fydar.Dev.WebApp.Internal.Authentication;
using MimeKit;
using MimeKit.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Fydar.Dev.WebApp.Internal;

public sealed record TicketAttachment(
    int Index,
    string FileName,
    string MediaType,
    long Size,
    MimeEntity Entity);

public static partial class TicketAttachments
{
    public const string DownloadPattern = "/ticket/{ticketId}/attachments/{index:int}";

    public static string DownloadUrl(string ticketId, int index)
    {
        return $"/ticket/{Uri.EscapeDataString(ticketId)}/attachments/{index}";
    }

    public static IReadOnlyList<TicketAttachment> Collect(MimeMessage message)
    {
        var referencedContentIds = ReferencedContentIds(message.HtmlBody);
        var attachments = new List<TicketAttachment>();

        Visit(message.Body);

        return attachments;

        void Visit(MimeEntity? entity)
        {
            switch (entity)
            {
                case Multipart multipart:
                    foreach (var child in multipart)
                    {
                        Visit(child);
                    }
                    break;

                case MessagePart messagePart when messagePart.Message != null:
                    attachments.Add(new TicketAttachment(
                        attachments.Count,
                        SafeFileName(messagePart.ContentDisposition?.FileName)
                            ?? (SafeFileName(messagePart.Message.Subject) ?? $"attachment-{attachments.Count + 1}") + ".eml",
                        messagePart.ContentType.MimeType,
                        Measure(messagePart.Message.WriteTo),
                        messagePart));
                    break;

                case MimePart part when IsAttachment(part, referencedContentIds):
                    attachments.Add(new TicketAttachment(
                        attachments.Count,
                        SafeFileName(part.FileName) ?? DefaultFileName(part, attachments.Count),
                        part.ContentType.MimeType,
                        part.Content == null ? 0 : Measure(part.Content.DecodeTo),
                        part));
                    break;
            }
        }
    }

    public static WebApplication MapTicketAttachmentEndpoints(this WebApplication app)
    {
        app.MapGet(DownloadPattern, async (
            string ticketId,
            int index,
            IEmailReaderService emailReaderService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            MimeMessage message;
            try
            {
                message = await emailReaderService.ReadEmailAsync(ticketId, cancellationToken);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return Results.NotFound();
            }

            var attachments = Collect(message);
            if (index < 0 || index >= attachments.Count)
            {
                return Results.NotFound();
            }

            var attachment = attachments[index];

            using var content = new MemoryStream();
            switch (attachment.Entity)
            {
                case MessagePart messagePart:
                    await messagePart.Message.WriteToAsync(content, cancellationToken);
                    break;

                case MimePart { Content: not null } part:
                    await part.Content.DecodeToAsync(content, cancellationToken);
                    break;
            }

            var headers = httpContext.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.CacheControl = "private, no-store";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers.ContentSecurityPolicy = "sandbox; default-src 'none'";

            return Results.File(
                content.ToArray(),
                "application/octet-stream",
                attachment.FileName);
        })
        .RequireAuthorization(GitHubAuthenticationDefaults.AuthorizationPolicy);

        return app;
    }

    private static bool IsAttachment(MimePart part, HashSet<string> referencedContentIds)
    {
        if (part.IsAttachment)
        {
            return true;
        }

        if (part.ContentId != null && referencedContentIds.Contains(part.ContentId.Trim('<', '>')))
        {
            return false;
        }

        if (part is TextPart && part.FileName == null)
        {
            return false;
        }

        return part is not MimeKit.Cryptography.ApplicationPkcs7Signature
            and not MimeKit.Cryptography.ApplicationPgpSignature;
    }

    private static HashSet<string> ReferencedContentIds(string? html)
    {
        HashSet<string> contentIds = [with(StringComparer.Ordinal)];

        if (html == null)
        {
            return contentIds;
        }

        foreach (Match match in ContentIdReference().Matches(html))
        {
            contentIds.Add(Uri.UnescapeDataString(match.Groups["id"].Value).Trim('<', '>'));
        }

        return contentIds;
    }

    private static long Measure(Action<Stream, CancellationToken> write)
    {
        using var measuring = new MeasuringStream();
        write(measuring, CancellationToken.None);
        return measuring.Length;
    }

    private static string DefaultFileName(MimePart part, int index)
    {
        string name = $"attachment-{index + 1}";

        return MimeTypes.TryGetExtension(part.ContentType.MimeType, out string? extension)
            ? name + extension
            : name;
    }

    private static string? SafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        int lastSeparator = fileName.LastIndexOfAny(['/', '\\']);
        if (lastSeparator >= 0)
        {
            fileName = fileName[(lastSeparator + 1)..];
        }

        var builder = new StringBuilder(fileName.Length);
        foreach (char character in fileName)
        {
            if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        string safe = builder.ToString().Trim().TrimEnd('.', ' ');

        return safe.Length == 0 ? null : safe;
    }

    [GeneratedRegex("""cid:(?<id>[^"'\s)>]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ContentIdReference();
}
