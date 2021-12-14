using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FutBot
{
    class Program
    {
        private static readonly List<FutBinPlayer> Players = new();
        private static readonly Dictionary<string, DateTime> Challenges = new();
        private static readonly List<string> VisitedChallenges = new();

        static async Task Main(string[] args)
        {
            //await Run();
            Constants.X_UT_SID = args[0];
            await OldSchool();
        }

        private static async Task OldSchool()
        {
            var futService = new FutService();
            var minPrice = 1000;
            var multiplier = 1;

            while (true)
            {
                Console.WriteLine("New Iteration");
                var auctions = await futService.GetPlayerAvailableAuctions("195864", 18000, minPrice);

                foreach (var availableAuction in auctions.OrderBy(a => a.BuyNowPrice))
                {
                    var auction =
                        await futService.BuyOrBidAuction(availableAuction.TradeId, availableAuction.BuyNowPrice);

                    if (auction == null)
                    {
                        Console.WriteLine($"Missed with {availableAuction.BuyNowPrice}");
                        continue;
                    }

                    Console.WriteLine($"Bought with {availableAuction.BuyNowPrice}");

                    await futService.ListPlayer(auction.ItemData.Id, price: 19750, bidPrice: 19500);
                }

                minPrice += multiplier * 100;

                if (minPrice >= 7000 || minPrice <= 1000)
                {
                    multiplier *= -1;
                }

                await Task.Delay(5000);
            }
        }

        private static async Task Inspect()
        {
            var challengeSolutionIds = GetChallengeSolutionIds();

            foreach (var challengeSolutionId in challengeSolutionIds)
            {
                Console.WriteLine($"Checking Challenge Solution {challengeSolutionId}");

                if (VisitedChallenges.Contains(challengeSolutionId))
                {
                    continue;
                }

                VisitedChallenges.Add(challengeSolutionId);

                var playerFutBinIds = GetChallengePlayerIds(challengeSolutionId);

                foreach (var playerFutBinId in playerFutBinIds)
                {
                    Console.WriteLine($"Checking Player {playerFutBinId}");

                    var player = Players.FirstOrDefault(p => p.FutBinId == playerFutBinId);

                    if (player is null)
                    {
                        player = await GetFutBinPlayer(playerFutBinId);
                        Players.Add(player);
                    }

                    player.Count++;
                }
            }

            foreach (var player in Players.Where(p => p.Rating < 75).OrderByDescending(p => p.Count))
            {
                Console.WriteLine($"{player} => Count: {player.Count}");
            }
        }

        private static async Task Run()
        {
            while (true)
            {
                Console.WriteLine("New Round");

                var futService = new FutService();

                var auctions = await futService.GetAuctionsInTrade();

                await DeleteSoldAuctions(auctions);

                await RelistExpiredAuctions(auctions);

                auctions = await futService.GetAuctionsInTrade();

                var needToBuy = 100 - auctions.Count;

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Need To Buy: {needToBuy}");

                if (needToBuy > 0)
                {
                    await BuyPlayers(needToBuy, auctions);
                }

                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Round Completed");

                await Task.Delay(1000 * 60);
            }
        }

        private static async Task BrazilianRun()
        {
            while (true)
            {
                Console.WriteLine("New Round");

                var futService = new FutService();

                var auctions = await futService.GetAuctionsInTrade();

                var soldAuctions = auctions.Where(a => a.Expires == -1 && a.TradeState == "closed").ToList();
                Console.WriteLine($"Sold Auctions: {soldAuctions.Count}");

                await DeleteSoldAuctions(auctions);

                await RelistExpiredAuctions(auctions);

                var needToBuy = 100 - auctions.Count;

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Need To Buy: {needToBuy}");

                /*if (needToBuy > 0)
                {
                    await BuyBrazilianPlayers(needToBuy);
                }*/

                await BidOnPlayers(needToBuy);

                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Round Completed");

                await Task.Delay(1000 * 30);
            }
        }

        private static async Task BidOnPlayers(int maxPlayerToSell)
        {
            var futService = new FutService();

            var watchList = await futService.GetAuctionsInWatchList();

            var doneDeals = watchList.Where(a => a.Expires == -1 && a.BidState == "highest").ToList();

            Console.WriteLine($"Done Deals: {doneDeals.Count}");

            var soldPlayers = 0;
            foreach (var bidAuction in doneDeals)
            {
                if (soldPlayers == maxPlayerToSell) break;

                Console.WriteLine("Selling Player");

                var sellPrice = await GetBestSellPrice(bidAuction);

                await futService.ListPlayer(bidAuction.ItemData.Id, sellPrice, 500);

                soldPlayers++;
            }

            var auctionsToDelete = watchList.Where(a => a.Expires == -1 && a.BidState != "highest").ToList();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Deleting {auctionsToDelete.Count} Players Missed");

            foreach (var auction in auctionsToDelete)
            {
                await futService.DeleteWatchedAuction(auction.TradeId);
            }

            if (watchList.Count - auctionsToDelete.Count + soldPlayers == 50) return;

            var possibleBids = 50 - watchList.Count + auctionsToDelete.Count - soldPlayers;

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"Need to Bid On {possibleBids} Players");

            var playerAuctions = await futService.GetCriteriaInBidRangeAuctions();

            if (playerAuctions.Count == 0) return;

            var inTimeRangeAuctions =
                playerAuctions.Where(a => a.Expires <= 60 && a.TradeState != "highest" && a.SuggestedBid <= 500)
                    .ToList();

            if (inTimeRangeAuctions.Count == 0)
            {
                return;
            }

            foreach (var auction in inTimeRangeAuctions.OrderBy(a => a.Expires).Take(5))
            {
                var bidAuction = await futService.BuyOrBidAuction(auction.TradeId, auction.SuggestedBid);

                if (bidAuction != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Successful Bid On Player With {auction.SuggestedBid}");
                    possibleBids--;

                    if (possibleBids == 0) break;
                }

                await Task.Delay(2000);
            }
        }

        private static async Task<int> GetBestSellPrice(Auction auction)
        {
            var futService = new FutService();

            var cheapestAuctions = await futService.GetPlayerAvailableAuctions(auction.ItemData.AssetId);
            var suggestedPrice = cheapestAuctions.Min(a => a.BuyNowPrice) - 50;
            Console.WriteLine($"Suggested Price: {suggestedPrice}");
            var sellPrice = Math.Max(suggestedPrice, 700);

            return sellPrice;
        }

        private static async Task DeleteSoldAuctions(List<Auction> auctions)
        {
            var futService = new FutService();

            var soldAuctions = auctions.Where(a => a.Expires == -1 && a.TradeState == "closed").ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Need To Delete: {soldAuctions.Count}");

            foreach (var auction in soldAuctions)
            {
                Console.WriteLine($"Deleting Player Sold With {auction.CurrentBid}");

                await futService.DeleteSoldAuction(auction.TradeId);
                await Task.Delay(1000);
            }
        }

        private static async Task RelistExpiredAuctions(List<Auction> auctions)
        {
            var futService = new FutService();

            var expiredAuctions = auctions.Where(a => a.Expires == -1 && a.TradeState == "expired").ToList();

            if (expiredAuctions.Count == 0) return;

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Need To Resubmit: {expiredAuctions.Count}");

            foreach (var auction in expiredAuctions)
            {
                /*if (auction.BuyNowPrice == 200)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Discarding One Player");
                    await futService.DiscardTrade(auction.ItemData.Id);
                    continue;
                }*/


                //var cheapestAuctions = await futService.GetPlayerAvailableAuctions(auction.ItemData.AssetId);

                //var sellPrice = Math.Max(cheapestAuctions.Min(a => a.BuyNowPrice) - 50, 200);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                await futService.SellPlayer(auction.ItemData.Id, price: auction.BuyNowPrice - 50,
                    bidPrice: auction.StartingBid - 50);

                await Task.Delay(5000);
            }
        }

        private static async Task BuyPlayers(int needToBuy, List<Auction> inTradeAuctions)
        {
            var futService = new FutService();

            var checkedPlayers = new List<string>();

            var challengeSolutionIds = GetChallengeSolutionIds();

            var index = 0;

            foreach (var challengeSolutionId in challengeSolutionIds.Take(2))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;

                if (Challenges.ContainsKey(challengeSolutionId))
                {
                    var lastVisitDate = Challenges[challengeSolutionId];

                    Console.WriteLine($"Already Visited Challenge {challengeSolutionId} on {lastVisitDate}");
                    if (lastVisitDate.AddMinutes(5) <= DateTime.Now)
                    {
                        Console.WriteLine("Retry");
                        Challenges[challengeSolutionId] = DateTime.Now;
                    }
                    else
                    {
                        Console.WriteLine("Skip");
                        continue;
                    }
                }
                else
                {
                    Challenges.Add(challengeSolutionId, DateTime.Now);
                    Console.WriteLine($"Visiting Challenge {challengeSolutionId}");
                }

                var playerFutBinIds = GetChallengePlayerIds(challengeSolutionId);

                foreach (var playerFutBinId in playerFutBinIds)
                {
                    if (checkedPlayers.Contains(playerFutBinId))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("Skipping Player");
                        continue;
                    }

                    checkedPlayers.Add(playerFutBinId);

                    var player = Players.FirstOrDefault(p => p.FutBinId == playerFutBinId);

                    if (player is null)
                    {
                        player = await GetFutBinPlayer(playerFutBinId);
                        Players.Add(player);
                    }

                    if (player.Rating >= 75) continue;

                    var playerInTradeAuctions =
                        inTradeAuctions.Where(a => a.ItemData.AssetId.ToString() == player.FifaId).ToList();

                    if (playerInTradeAuctions.Count >= 10) continue;

                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"Checking Player {player}");
                    var auctions = await futService.GetPlayerAvailableAuctions(player.FifaId, 250);

                    var maxBuy = 3 - playerInTradeAuctions.Count;

                    var sellPrice = 500;

                    foreach (var playerAuction in auctions.OrderBy(a => a.BuyNowPrice).Take(Math.Min(2, maxBuy)))
                    {
                        var auction =
                            await futService.BuyOrBidAuction(playerAuction.TradeId, playerAuction.BuyNowPrice);

                        if (auction == null)
                        {
                            continue;
                        }

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Player Bought With {auction.BuyNowPrice}");

                        await futService.ListPlayer(auction.ItemData.Id, sellPrice);

                        index++;

                        if (index == needToBuy) break;

                        await Task.Delay(500);
                    }

                    if (index == needToBuy) break;

                    await Task.Delay(5000);
                }

                if (index == needToBuy) break;
            }
        }


        private static int counter = 0;

        private static async Task BuyBrazilianPlayers(int needToBuy)
        {
            counter++;

            var futService = new FutService();
            var auctions =
                await futService.GetCriteriaAvailableAuctions(maxPrice: 500, minBid: counter % 2 == 0 ? -1 : 150);

            var index = 0;

            foreach (var playerAuction in auctions.OrderBy(a => a.BuyNowPrice).ThenByDescending(a => a.Expires))
            {
                var auction =
                    await futService.BuyOrBidAuction(playerAuction.TradeId, playerAuction.BuyNowPrice);

                if (auction == null)
                {
                    Console.WriteLine($"Player Missed");
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Player Bought With {auction.BuyNowPrice}");

                await futService.ListPlayer(auction.ItemData.Id, 800);

                index++;

                if (index == needToBuy) break;
            }
        }

        private static List<string> GetChallengeSolutionIds()
        {
            var index = 0;

            while (index < 3)
            {
                try
                {
                    var challengeSolutionIds = new List<string>();
                    var htmlHelper = new HtmlHelper();
                    var htmlDoc =
                        htmlHelper.GetHtmlContent(
                            "https://www.futbin.com/squad-building-challenges/ALL/690/CONMEBOL%20Libertadores%20Challenge");

                    var nodes = htmlDoc.DocumentNode.SelectNodes("//a[@class='squad_url']");

                    if (nodes == null)
                    {
                        return challengeSolutionIds;
                    }

                    foreach (var node in nodes)
                    {
                        var challengeSolutionUrl = node.Attributes["href"].Value;
                        var regex = new Regex("/22/squad/(?<ChallengeSolutionId>[\\d]*)/sbc");
                        var match = regex.Match(challengeSolutionUrl);
                        if (!match.Success)
                        {
                            throw new HtmlParseException();
                        }

                        var challengeSolutionId = match.Groups["ChallengeSolutionId"].Value;

                        challengeSolutionIds.Add(challengeSolutionId);
                    }

                    return challengeSolutionIds;
                }
                catch (Exception)
                {
                    Task.Delay(5000);
                    index++;
                }
            }

            throw new HtmlParseException();
        }

        private static List<string> GetChallengePlayerIds(string challengeSolutionId)
        {
            var index = 0;

            while (index < 3)
            {
                try
                {
                    var players = new List<string>();
                    var htmlHelper = new HtmlHelper();
                    var htmlDoc =
                        htmlHelper.GetHtmlContent($"https://www.futbin.com/22/squad/{challengeSolutionId}/sbc");

                    var nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='card cardnum ui-droppable added']");

                    if (nodes == null)
                    {
                        return players;
                    }

                    foreach (var node in nodes)
                    {
                        var div = node.SelectSingleNode(".//div[@class='cardetails']");
                        var linkNode = div.SelectSingleNode(".//a");
                        var playerUrl = linkNode.Attributes["href"].Value;

                        var regex = new Regex("/22/player/(?<PlayerId>[\\d]*)/.*");
                        var match = regex.Match(playerUrl);
                        if (!match.Success)
                        {
                            throw new HtmlParseException();
                        }

                        var playerId = match.Groups["PlayerId"].Value;

                        players.Add(playerId);
                    }

                    return players;
                }
                catch (Exception)
                {
                    index++;
                    Task.Delay(5000);
                }
            }

            throw new HtmlParseException();
        }

        private static async Task<FutBinPlayer> GetFutBinPlayer(string playerFutBinId)
        {
            var index = 0;
            while (index < 3)
            {
                try
                {
                    var htmlHelper = new HtmlHelper();
                    var htmlDoc = htmlHelper.GetHtmlContent($"https://www.futbin.com/22/player/{playerFutBinId}");


                    var name = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"Player-card\"]/div[3]").InnerText;
                    var rating = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"Player-card\"]/div[2]").InnerText;
                    var position = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"Player-card\"]/div[4]").InnerText;
                    var pageInfoDiv = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"page-info\"]");
                    var fifaId = pageInfoDiv.Attributes["data-baseid"].Value;


                    return new FutBinPlayer
                    {
                        FutBinId = playerFutBinId,
                        FifaId = fifaId,
                        Name = name,
                        Position = position,
                        Rating = int.Parse(rating)
                    };
                }
                catch (Exception)
                {
                    await Task.Delay(5000);
                    index++;
                }
            }

            throw new HtmlParseException();
        }
    }
}