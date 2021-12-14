using System;
using System.Net;
using System.Threading;
using HtmlAgilityPack;

namespace FutBot
{
    public class HtmlHelper : IHtmlHelper
    {
        public HtmlDocument GetHtmlContent(string url)
        {
            string content = string.Empty;
            var tries = 0;
            var done = false;

            while (!done && tries < 3)
            {
                try
                {
                    var w = new WebClient();
                    w.Headers.Add("User-Agent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");
                    content = w.DownloadString(url);
                    done = true;
                }
                catch (Exception e)
                {
                    throw;
                    tries++;
                    Thread.Sleep(5000);
                }
            }

            if (!done)
            {
                throw new HtmlParseException();
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            return htmlDoc;
        }
    }
}