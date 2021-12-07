using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace FutBot
{
    public class FutService
    {
        public async Task<List<Auction>> GetAuctionsInTrade()
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("/ut/game/fifa22/tradepile");

            request.AddFutHeaders();

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);
            return response.AuctionInfo;
        }

        public async Task<List<Auction>> GetAuctionsInWatchList()
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("/ut/game/fifa22/watchlist");

            request.AddFutHeaders();

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);
            return response.AuctionInfo;
        }

        public async Task<List<PlayerData>> GetPurchasedPlayersList()
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("/ut/game/fifa22/purchased/items");

            request.AddFutHeaders();

            var response = await client.GetAsync<PurchasedResponse>(request, CancellationToken.None);
            return response.ItemData;
        }

        public async Task<List<Auction>> GetCriteriaAvailableAuctions(int maxPrice = 300, int minb = 150,
            int minBid = -1)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/transfermarket");

            request.AddFutHeaders();


            request.AddQueryParameter("num", "21");
            request.AddQueryParameter("start", "0");
            request.AddQueryParameter("type", "player");
            //request.AddQueryParameter("pos", "CB");
            request.AddQueryParameter("rarityIds", "1");
            request.AddQueryParameter("lev", "silver");
            request.AddQueryParameter("nat", "14");
            //request.AddQueryParameter("leag", "16");
            //request.AddQueryParameter("minb", minb.ToString());
            request.AddQueryParameter("maxb", maxPrice.ToString());
            if (minBid != -1)
            {
                request.AddQueryParameter("micr", minBid.ToString());
            }

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);

            return response.AuctionInfo;
        }

        public async Task<Auction> BuyOrBidAuction(long tradeId, int price)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest($"/ut/game/fifa22/trade/{tradeId}/bid");

            request.AddFutHeaders();

            request.AddJsonBody(new {bid = price});

            try
            {
                var auctionResponse = await client.PutAsync<AuctionResponse>(request);

                if (auctionResponse != null)
                {
                    return auctionResponse.AuctionInfo[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed To Buy: {e.Message}");
            }

            return null;
        }

        public async Task<List<Auction>> GetPlayerAvailableAuctions(long playerId)
        {
            var maxPrice = 1000;
            var auctions = new List<Auction>();

            while (auctions.Count == 0)
            {
                var client = new RestClient("https://utas.external.s2.fut.ea.com");

                var request = new RestRequest("ut/game/fifa22/transfermarket");

                request.AddFutHeaders();

                request.AddQueryParameter("num", "21");
                request.AddQueryParameter("start", "0");
                request.AddQueryParameter("type", "player");
                request.AddQueryParameter("maskedDefId", playerId.ToString());
                request.AddQueryParameter("maxb", maxPrice.ToString());

                var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);

                auctions = response.AuctionInfo;

                maxPrice += 1000;
            }

            return auctions;
        }

        public async Task<List<Auction>> GetPlayerCheapAuctions(long playerId, int maxPrice = 300)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/transfermarket");

            request.AddFutHeaders();

            request.AddQueryParameter("num", "21");
            request.AddQueryParameter("start", "0");
            request.AddQueryParameter("type", "player");
            request.AddQueryParameter("maskedDefId", playerId.ToString());
            request.AddQueryParameter("maxb", maxPrice.ToString());

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);

            return response.AuctionInfo;
        }

        public async Task<List<Auction>> GetPlayerInBidRangeAuctions(long playerId, int maxPrice = 300)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/transfermarket");

            request.AddFutHeaders();

            request.AddQueryParameter("num", "21");
            request.AddQueryParameter("start", "0");
            request.AddQueryParameter("type", "player");
            request.AddQueryParameter("maskedDefId", playerId.ToString());
            request.AddQueryParameter("macr", maxPrice.ToString());

            var response = await client.GetAsync<AuctionResponse>(request, CancellationToken.None);

            return response.AuctionInfo;
        }

        public async Task ListPlayer(long itemId, int price, int time = 3600)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var sendToPileRequest = new RestRequest("ut/game/fifa22/item");

            sendToPileRequest.AddFutHeaders();

            sendToPileRequest.AddJsonBody(new
                {itemData = new List<dynamic> {new {id = itemId, pile = "trade"}}});

            client.Put(sendToPileRequest);

            await SellPlayer(itemId, price, time);
        }

        public async Task SellPlayer(long itemId, int price, int time = 3600)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/auctionhouse");

            request.AddFutHeaders();

            var bidPrice = price > 1000 ? price - 100 : price - 50;

            request.AddJsonBody(new
            {
                itemData = new {id = itemId},
                startingBid = bidPrice,
                duration = time,
                buyNowPrice = price
            });

            var response = await client.ExecutePostAsync(request, CancellationToken.None);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                Program.Anomalies++;
                Console.WriteLine("Issue When Posting");
            }
            else
            {
                Console.WriteLine($"Posted With: {price}");
            }
        }

        public async Task DeleteSoldAuction(long tradeId)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest($"ut/game/fifa22/trade/{tradeId}");

            request.AddFutHeaders();

            client.Delete(request);

            await Task.CompletedTask;
        }

        public async Task DeleteWatchedAuction(long tradeId)
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest($"ut/game/fifa22/watchlist?tradeId={tradeId}");

            request.AddFutHeaders();

            client.Delete(request);

            await Task.CompletedTask;
        }

        public async Task ResubmitAuction(Auction auction)
        {
            var availableAuctions = await GetPlayerAvailableAuctions(auction.ItemData.AssetId);

            await Task.Delay(1000);

            if (availableAuctions.Count == 0)
            {
                Console.WriteLine("Good Catch!!!");
                await ListPlayer(auction.ItemData.Id, 1000);
            }
            else
            {
                Console.WriteLine(
                    $"Available Count: {availableAuctions.Count}, Min Price: {availableAuctions.Min(a => a.BuyNowPrice)}");

                var bestPrice = availableAuctions.Min(a => a.BuyNowPrice);
                var sellPrice = bestPrice > 1000 ? bestPrice - 100 : bestPrice - 50;

                await ListPlayer(auction.ItemData.Id, Math.Max(sellPrice, 500));
            }
        }

        public async Task SubmitAuction(Auction auction)
        {
            var availableAuctions = await GetPlayerAvailableAuctions(auction.ItemData.AssetId);

            await Task.Delay(1000);

            if (availableAuctions.Count == 0)
            {
                Console.WriteLine("Good Catch!!!");
                await ListPlayer(auction.ItemData.Id, 2000);
            }
            else
            {
                var bestPrice = availableAuctions.Min(a => a.BuyNowPrice);
                var sellPrice = bestPrice > 1000 ? bestPrice - 100 : bestPrice - 50;

                Console.WriteLine(
                    $"Available Count: {availableAuctions.Count}, Min Price: {availableAuctions.Min(a => a.BuyNowPrice)}");
                await SellPlayer(auction.ItemData.Id, Math.Max(sellPrice, 300));
            }
        }

        public async Task ResubmitList()
        {
            var client = new RestClient("https://utas.external.s2.fut.ea.com");

            var request = new RestRequest("ut/game/fifa22/auctionhouse/relist ");

            request.AddFutHeaders();

            client.Put(request);

            await Task.CompletedTask;
        }
    }
}