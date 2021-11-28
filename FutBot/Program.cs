using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace FutBot
{
    public static class Constants
    {
        //public const string X_UT_SID = "d41370ae-18de-48f4-8f0b-1627c07bb527";
        public static string X_UT_SID = "d41370ae-18de-48f4-8f0b-1627c07bb527";
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Constants.X_UT_SID = args[0];
            /*var players = new List<Player>
            {
                new(182160, "Knudtzon", "LM", 64),
                new(228804, "De Sart", "CM", 70),
                new(259916, "Granath", "CB", 60),
                new(242079, "Bistrovic", "CDM", 70),
                new(229910, "Ingelsson", "CM", 67),
                new(201526, "Bech", "RM", 66),
                new(241645, "Torp", "CAM", 66),
                new(206520, "Kim Won Sik", "CDM", 63),
                new(257771, "Juranovic", "RM", 71),
                new(169708, "Hutchinson", "CB", 68),
                new(237930, "Hausner", "CB", 68),
                new(239179, "Arsenic", "CB", 66),
                new(205929, "Thomsen", "LM", 65),
                new(162512, "Cuthbert", "CB", 63),
                new(193091, "Nuytinck", "CB", 74),
                new(232000, "Lee Seung Woo", "LM", 67),
                new(221875, "Son Jun Ho", "CDM", 71),
                new(248471, "Taferner", "CM", 67),
                new(259853, "Mukhin", "CM", 68),
                new(226518, "Milic", "CB", 67),
                new(170923, "Eschenko", "RB", 70),
                new(202043, "Holmberg", "ST", 67),
                new(224855, "Byers", "CM", 66),
                new(233791, "Maksimenko", "GK", 74),
                new(228802, "Zhivoglyadov", "RB", 72),
                new(232695, "Sbuttoni", "CB", 68),
                new(163156, "Keogh", "CB", 67),
                new(235544, "Memišević", "CDM", 70),
                new(251446, "Mihaj", "CB", 65),
                new(247042, "Lofgren", "CB", 61),
                new(202660, "Sjostedt", "CB", 62),
                new(220847, "Maertens", "CM", 69),
                new(208422, "Sanusi", "CM", 69)
            };

            var auctions = await GetAuctionsInTrade();

            foreach (var auction in auctions)
            {
                var player = players.FirstOrDefault(p => p.Id == auction.ItemData.AssetId);
                if (player == null) continue;

                player.Count++;
            }

            foreach (var player in players.OrderBy(p => p.Count))
            {
                await Task.Delay(3000);

                var availableAuctions = await GetPlayerAvailableAuctions(player.Id);

                if (!availableAuctions.Any())
                {
                    Console.WriteLine(
                        $"Player: {player.Name}, In Trade Count: {player.Count}, Not Available");
                }
                else
                {
                    Console.WriteLine(
                        $"Player: {player.Name}, In Trade Count: {player.Count}, Available Count: {availableAuctions.Count}, Min Price: {availableAuctions.Min(a => a.BuyNowPrice)}");
                }
            }*/

            var doWork = true;
            var iteration = 0;
            var minb = 150;

            while (doWork)
            {
                iteration++;
                minb += minb < 1000 ? 50 : 100;
                await Task.Delay(2000);

                Console.WriteLine($"Iteration: {iteration}");

                var auctions = await GetCriteriaAvailableAuctions(minb);

                if (auctions == null)
                {
                    Console.WriteLine("Not found");
                    continue;
                }

                var auction = auctions.LastOrDefault();

                if (auction == null)
                {
                    Console.WriteLine("Not found");
                    continue;
                }

                doWork = !await BuyAuction(auction.TradeId, auction.BuyNowPrice);
            }
        }

        private static async Task<bool> BuyAuction(long tradeId, int price)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest($"/ut/game/fifa22/trade/{tradeId}/bid");

            request.AddFutHeaders();

            request.AddJsonBody(new {bid = price});

            var response = client.Put(request);
            Console.WriteLine(response.Content);

            if (response.IsSuccessful)
            {
                Console.WriteLine($"{response.StatusCode}: {response.StatusDescription}");
                return true;
            }

            await Task.CompletedTask;

            return false;
        }

        private static async Task<List<Auction>> GetAuctionsInTrade()
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("/ut/game/fifa22/tradepile");

            request.AddFutHeaders();

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);
            return response.AuctionInfo;
        }

        private static async Task<List<Auction>> GetCriteriaAvailableAuctions(int minb)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/transfermarket");

            request.AddFutHeaders();
            
            //https://utas.external.s2.fut.ea.com/ut/game/fifa22/transfermarket?num=21&start=0&type=player&pos=CB&rarityIds=1&lev=gold&leag=16&maxb=2800

            request.AddQueryParameter("num", "21");
            request.AddQueryParameter("start", "0");
            request.AddQueryParameter("type", "player");
            //request.AddQueryParameter("pos", "CB");
            request.AddQueryParameter("rarityIds", "1");
            request.AddQueryParameter("lev", "gold");
            request.AddQueryParameter("leag", "16");
            request.AddQueryParameter("minb", minb.ToString());
            request.AddQueryParameter("maxb", "1500");

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);

            return response.AuctionInfo;
        }

        private static async Task<List<Auction>> GetPlayerAvailableAuctions(long playerId)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/transfermarket");

            request.AddFutHeaders();

            request.AddQueryParameter("num", "21");
            request.AddQueryParameter("start", "0");
            request.AddQueryParameter("type", "player");
            request.AddQueryParameter("maskedDefId", playerId.ToString());
            request.AddQueryParameter("maxb", "500");

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);

            return response.AuctionInfo;
        }
    }

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

    public class Player
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public int Rating { get; set; }
        public int Count { get; set; }

        public Player(long id, string name, string position, int rating)
        {
            Id = id;
            Name = name;
            Position = position;
            Rating = rating;
        }
    }

    public class PlayerData
    {
        public long AssetId { get; set; }
    }

    public class AuctionResponse
    {
        public List<Auction> AuctionInfo { get; set; }
    }

    public class Auction
    {
        public int BuyNowPrice { get; set; }
        public long TradeId { get; set; }
        public long Expires { get; set; }
        public PlayerData ItemData { get; set; }
    }
}


/*
//var options = new JsonSerializerOptions { WriteIndented = true };
            //var jsonString = JsonSerializer.Serialize(players, options);
            //await File.WriteAllTextAsync("xyz.json", jsonString);

            var jsonString = await File.ReadAllTextAsync(@"players.json");
            var players = JsonSerializer.Deserialize<List<Player>>(jsonString);

*/