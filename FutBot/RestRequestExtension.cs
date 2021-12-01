using RestSharp;

namespace FutBot
{
    public static class RestRequestExtension
    {
        public static void AddFutHeaders(this RestRequest request)
        {
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br");
            request.AddHeader("Accept-Language", "en-US,en;q=0.9,fr-FR;q=0.8,fr;q=0.7,ar;q=0.6");
            request.AddHeader("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Referer", "https://www.ea.com/");
            request.AddHeader("X-UT-SID", Constants.X_UT_SID);
            request.AddHeader("Origin", "https://www.ea.com");
            request.AddHeader("sec-ch-ua",
                "\" Not A;Brand\";v= \"99\", \"Chromium\";v=\"96\", \"Google Chrome\";v=\"96\"");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("sec-ch-ua-platform", "\"Windows\"");
            request.AddHeader("Sec-Fetch-Dest", "empty");
            request.AddHeader("Sec-Fetch-Mode", "cors");
            request.AddHeader("Sec-Fetch-Site", "same-site");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Host", "utas.external.s2.fut.ea.com");
        }
    }
}