using HtmlAgilityPack;

namespace FutBot
{
    public interface IHtmlHelper
    {
        HtmlDocument GetHtmlContent(string url);
    }
}