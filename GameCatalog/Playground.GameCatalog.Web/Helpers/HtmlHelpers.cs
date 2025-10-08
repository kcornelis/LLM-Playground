using Microsoft.AspNetCore.Components;

namespace Playground.GameCatalog.Web.Helpers;

public static class HtmlHelpers
{
    public static MarkupString ToMarkup(string? s) => new(s ?? string.Empty);
}