using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using Microsoft.AspNetCore.Components;
using MimeKit;

namespace Fydar.Dev.WebApp.Internal;

/// <summary>
/// Sanitizes the HTML body of an inbound email so the ticket page can render it as markup.
/// </summary>
/// <remarks>
/// Anybody can send an email to the inbox behind the ticket page, so its body is attacker
/// controlled markup that ends up inside a page on our own origin. Everything outside of the
/// allowed tags, attributes, style properties and URI schemes is dropped.
/// </remarks>
internal sealed class TicketHtmlSanitizer
{
    // Data entry controls carry no content of their own, and there is nothing on the ticket
    // page for an email to submit.
    private static readonly string[] disallowedTags = [
        "button",
        "datalist",
        "input",
        "keygen",
        "optgroup",
        "option",
        "output",
        "select",
        "textarea"];

    // Attributes that either aim a form submission somewhere, or take over the reader's
    // keyboard, pointer or clipboard.
    private static readonly string[] disallowedAttributes = [
        "accesskey",
        "action",
        "autocomplete",
        "contenteditable",
        "draggable",
        "dropzone",
        "enctype",
        "method",
        "novalidate",
        "tabindex",
        "target"];

    // Positioning would let an email lift its content out of the ticket body and lay it over
    // the site's own chrome.
    private static readonly string[] disallowedCssProperties = [
        "position",
        "z-index"];

    // On top of the http and https allowed by default; both are inert until clicked.
    // reaches the network. Both are held to images by RestrictInlineData.
    private static readonly string[] allowedSchemes = [
        "mailto",
        "tel",
        "cid",
        "data"];

    private static readonly string[] inlineImageMediaTypes = [
        "image/avif",
        "image/bmp",
        "image/gif",
        "image/jpeg",
        "image/png",
        "image/webp"];

    public const string BlockedAttribute = "data-blocked-remote";

    private readonly HtmlSanitizer trustingSanitizer;
    private readonly HtmlSanitizer blockingSanitizer;

    public TicketHtmlSanitizer()
    {
        trustingSanitizer = CreateSanitizer();

        blockingSanitizer = CreateSanitizer();
        blockingSanitizer.PostProcessNode += BlockRemoteContent;
    }

    /// <summary>
    /// Sanitizes an email's HTML body into markup that is safe to render on the ticket page.
    /// </summary>
    /// <param name="message">The email being rendered.</param>
    /// <param name="trustRemoteContent">
    /// Whether the email may reference content hosted elsewhere. Fetching it tells the sender
    /// the ticket was opened, which is how tracking pixels report back, so it is only done once
    /// the reader has decided the sender is worth trusting. Images carried by the email itself
    /// are shown either way.
    /// </param>
    /// <returns>The sanitized markup, and whether any remote content was withheld from it.</returns>
    public SanitizedTicketHtml Sanitize(
        MimeMessage message,
        bool trustRemoteContent)
    {
        string? html = message.HtmlBody;
        if (string.IsNullOrEmpty(html))
        {
            return default;
        }

        // Configuring a sanitizer is what isn't thread safe; sanitizing from several requests at
        // once is fine.
        string sanitized = trustRemoteContent
            ? trustingSanitizer.Sanitize(html)
            : blockingSanitizer.Sanitize(html);

        bool blockedRemoteContent = sanitized.Contains(BlockedAttribute, StringComparison.Ordinal);

        if (sanitized.Contains("cid:", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = EmbedInlineImages(sanitized, message);
        }

        return new SanitizedTicketHtml((MarkupString)sanitized, blockedRemoteContent);
    }

    /// <summary>
    /// Replaces the 'cid:' references an email uses for its own attached images with the image
    /// itself, which the page can then render without asking anybody for anything.
    /// </summary>
    private static string EmbedInlineImages(
        string html,
        MimeMessage message)
    {
        var document = new HtmlParser().ParseDocument(html);
        var images = document.QuerySelectorAll("img[src]");

        Dictionary<string, MimePart>? parts = null;

        foreach (var image in images)
        {
            string source = image.GetAttribute("src") ?? string.Empty;
            if (!source.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parts ??= CollectInlineImages(message);

            string contentId = Uri.UnescapeDataString(source["cid:".Length..]).Trim('<', '>');

            if (parts.TryGetValue(contentId, out var part))
            {
                image.SetAttribute("src", ToDataUrl(part));
            }
            else
            {
                image.RemoveAttribute("src");
            }
        }

        return parts == null
            ? html
            : document.Body?.InnerHtml ?? html;
    }

    private static Dictionary<string, MimePart> CollectInlineImages(
        MimeMessage message)
    {
        var parts = new Dictionary<string, MimePart>(StringComparer.Ordinal);

        foreach (var entity in message.BodyParts)
        {
            if (entity is not MimePart part
                || part.ContentId == null
                || !IsInlineImageMediaType(part.ContentType.MimeType))
            {
                continue;
            }

            parts.TryAdd(part.ContentId.Trim('<', '>'), part);
        }

        return parts;
    }

    private static string ToDataUrl(
        MimePart part)
    {
        using var content = new MemoryStream();
        part.Content.DecodeTo(content);

        return $"data:{part.ContentType.MimeType};base64,{Convert.ToBase64String(content.ToArray())}";
    }

    private static bool IsInlineImageMediaType(
        string? mediaType)
    {
        return mediaType != null
            && inlineImageMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        foreach (string disallowedTag in disallowedTags)
        {
            sanitizer.AllowedTags.Remove(disallowedTag);
        }
        foreach (string disallowedAttribute in disallowedAttributes)
        {
            sanitizer.AllowedAttributes.Remove(disallowedAttribute);
        }
        foreach (string disallowedCssProperty in disallowedCssProperties)
        {
            sanitizer.AllowedCssProperties.Remove(disallowedCssProperty);
        }
        foreach (string allowedScheme in allowedSchemes)
        {
            sanitizer.AllowedSchemes.Add(allowedScheme);
        }

        sanitizer.FilterUrl += RequireAbsoluteUrl;
        sanitizer.PostProcessNode += RestrictInlineData;
        sanitizer.PostProcessNode += IsolateLink;

        return sanitizer;
    }

    private static void BlockRemoteContent(
        object? sender,
        PostProcessNodeEventArgs eventArgs)
    {
        if (eventArgs.Node is not IElement element)
        {
            return;
        }

        // An image's source is the usual tracking pixel, but a style can fetch just as quietly
        // through background-image, list-style-image, cursor or border-image. Everything else
        // that reaches out - the background attribute, srcset, iframes, objects - was already
        // dropped by the allowlist. What the email carries with it is left alone: cid and data
        // URLs are answered from the message itself, so rendering them tells the sender nothing.
        if (element is IHtmlImageElement
            && element.GetAttribute("src") is string source
            && !IsCarriedByEmail(source))
        {
            element.RemoveAttribute("src");
            element.SetAttribute(BlockedAttribute, "");
        }

        string? style = element.GetAttribute("style");
        if (style != null
            && style.Contains("url(", StringComparison.OrdinalIgnoreCase))
        {
            string[] declarations = style.Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            string[] kept = [.. declarations.Where(declaration => !FetchesRemoteContent(declaration))];

            if (kept.Length == declarations.Length)
            {
                return;
            }

            if (kept.Length == 0)
            {
                element.RemoveAttribute("style");
            }
            else
            {
                element.SetAttribute("style", string.Join("; ", kept));
            }

            element.SetAttribute(BlockedAttribute, "");
        }
    }

    private static bool FetchesRemoteContent(
        string declaration)
    {
        int url = declaration.IndexOf("url(", StringComparison.OrdinalIgnoreCase);
        if (url == -1)
        {
            return false;
        }

        return !IsCarriedByEmail(declaration[(url + "url(".Length)..].TrimStart('"', '\'', ' '));
    }

    private static bool IsCarriedByEmail(
        string url)
    {
        string trimmed = url.TrimStart();

        return trimmed.StartsWith("cid:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static void RestrictInlineData(
        object? sender,
        PostProcessNodeEventArgs eventArgs)
    {
        // A data URL is only ever wanted here as an image the email carried with it. Anywhere
        // else - an anchor's href above all - it is a document from a stranger that the browser
        // would treat as coming from this site.
        if (eventArgs.Node is not IElement element)
        {
            return;
        }

        foreach (var attribute in element.Attributes.ToArray())
        {
            if (!attribute.Value.TrimStart().StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (element is IHtmlImageElement
                && attribute.Name.Equals("src", StringComparison.OrdinalIgnoreCase)
                && IsInlineImageMediaType(ReadMediaType(attribute.Value)))
            {
                continue;
            }

            element.RemoveAttribute(attribute.Name);
        }
    }

    private static string? ReadMediaType(
        string dataUrl)
    {
        string value = dataUrl.TrimStart()["data:".Length..];
        int end = value.IndexOfAny([';', ',']);

        return end == -1 ? null : value[..end];
    }

    private static void RequireAbsoluteUrl(
        object? sender,
        FilterUrlEventArgs eventArgs)
    {
        // A relative URL in an email has no base to resolve against, so email clients leave it
        // broken. Rendered here it would resolve against this site instead, quietly aiming an
        // untrusted link at our own origin.
        if (eventArgs.SanitizedUrl != null
            && !Uri.IsWellFormedUriString(eventArgs.SanitizedUrl, UriKind.Absolute))
        {
            eventArgs.SanitizedUrl = null;
        }
    }

    private static void IsolateLink(
        object? sender,
        PostProcessNodeEventArgs eventArgs)
    {
        // Runs after the node has been sanitized, so this replaces whatever the email asked for.
        if (eventArgs.Node is IHtmlAnchorElement anchor
            && anchor.HasAttribute("href"))
        {
            anchor.SetAttribute("target", "_blank");
            anchor.SetAttribute("rel", "noopener noreferrer nofollow");
        }
    }
}

/// <summary>
/// An email's HTML body, sanitized for rendering on the ticket page.
/// </summary>
/// <param name="Html">The markup that is safe to render.</param>
/// <param name="BlockedRemoteContent">
/// Whether anything the email would have fetched from elsewhere was withheld.
/// </param>
internal readonly record struct SanitizedTicketHtml(
    MarkupString Html,
    bool BlockedRemoteContent);
