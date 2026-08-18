using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Components;

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
    private static readonly string[] allowedSchemes = [
        "mailto",
        "tel"];

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
    /// <param name="html">The HTML body of the email.</param>
    /// <param name="trustRemoteContent">
    /// Whether the email may reference content hosted elsewhere. Fetching it tells the sender
    /// the ticket was opened, which is how tracking pixels report back, so it is only done once
    /// the reader has decided the sender is worth trusting.
    /// </param>
    /// <returns>The sanitized markup, and whether any remote content was withheld from it.</returns>
    public SanitizedTicketHtml Sanitize(
        string? html,
        bool trustRemoteContent)
    {
        if (string.IsNullOrEmpty(html))
        {
            return default;
        }

        // Configuring a sanitizer is what isn't thread safe; sanitizing from several requests at
        // once is fine.
        string sanitized = trustRemoteContent
            ? trustingSanitizer.Sanitize(html)
            : blockingSanitizer.Sanitize(html);

        return new SanitizedTicketHtml(
            (MarkupString)sanitized,
            sanitized.Contains(BlockedAttribute, StringComparison.Ordinal));
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
        // dropped by the allowlist, and data: URIs never make a request.
        if (element is IHtmlImageElement
            && element.HasAttribute("src"))
        {
            element.RemoveAttribute("src");
            element.SetAttribute(BlockedAttribute, "");
        }

        string? style = element.GetAttribute("style");
        if (style != null
            && style.Contains("url(", StringComparison.OrdinalIgnoreCase))
        {
            string remaining = string.Join(
                ';',
                style
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(declaration => !declaration.Contains("url(", StringComparison.OrdinalIgnoreCase)));

            if (string.IsNullOrEmpty(remaining))
            {
                element.RemoveAttribute("style");
            }
            else
            {
                element.SetAttribute("style", remaining);
            }

            element.SetAttribute(BlockedAttribute, "");
        }
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
