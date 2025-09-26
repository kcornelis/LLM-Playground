using Microsoft.AspNetCore.Components;

namespace Playground.Web.Helpers;

public static class HtmlHelpers
{
    public static MarkupString ToMarkup(string? s) => new MarkupString(s ?? string.Empty);
}