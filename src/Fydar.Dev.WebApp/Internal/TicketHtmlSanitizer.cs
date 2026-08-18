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

    private readonly HtmlSanitizer sanitizer;

    public TicketHtmlSanitizer()
    {
        sanitizer = new HtmlSanitizer();

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
    }

    /// <summary>
    /// Sanitizes an email's HTML body into markup that is safe to render on the ticket page.
    /// </summary>
    /// <param name="html">The HTML body of the email.</param>
    /// <returns>The sanitized markup.</returns>
    public MarkupString Sanitize(
        string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return default;
        }

        // Configuring the sanitizer is what isn't thread safe; sanitizing from several requests
        // at once is fine.
        return (MarkupString)sanitizer.Sanitize(html);
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
