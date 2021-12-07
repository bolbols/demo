using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FutBot
{
    class OldProgram
    {
        private static async Task TargetingStrategy()
        {
            var futService = new FutService();

            var players = new List<Player>
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

            var auctions = await futService.GetAuctionsInTrade();

            foreach (var auction in auctions)
            {
                var player = players.FirstOrDefault(p => p.Id == auction.ItemData.AssetId);
                if (player == null) continue;

                player.InSales++;
            }

            foreach (var player in players.OrderBy(p => p.InSales))
            {
                await Task.Delay(3000);

                var availableAuctions = await futService.GetPlayerAvailableAuctions(player.Id);

                player.Available = availableAuctions.Count;
                player.MinPrice = availableAuctions.Min(a => a.BuyNowPrice);
                player.MaxPrice = availableAuctions.Max(a => a.BuyNowPrice);
                player.AveragePrice =
                    ((int) Math.Floor(availableAuctions.Sum(a => a.BuyNowPrice) / availableAuctions.Count / 50.0) + 1) *
                    50;
            }

            foreach (var player in players.OrderBy(p => p.Available))
            {
                Console.WriteLine(player);
            }
        }

        private static async Task MassBuyingStrategy()
        {
            while (true)
            {
                Console.WriteLine("New Round");

                var completed = false;

                var addMinBid = true;

                var futService = new FutService();

                var myAuctions = await futService.GetAuctionsInTrade();

                var soldAuctions = myAuctions.Where(a => a.Expires == -1 && a.TradeState == "closed").ToList();
                var expiredAuctions = myAuctions.Where(a => a.Expires == -1 && a.TradeState == "expired").ToList();
                var idleAuctions = myAuctions.Where(a => a.Expires == 0).ToList();

                Console.WriteLine($"Need To Delete: {soldAuctions.Count}");

                foreach (var auction in soldAuctions)
                {
                    await futService.DeleteSoldAuction(auction.TradeId);
                    await Task.Delay(1000);
                }

                Console.WriteLine($"Need To Resubmit: {expiredAuctions.Count}");

                foreach (var auction in expiredAuctions)
                {
                    await futService.ResubmitAuction(auction);
                    await Task.Delay(1000);
                }

                Console.WriteLine($"Need To Submit: {idleAuctions.Count}");

                foreach (var auction in idleAuctions)
                {
                    await futService.SubmitAuction(auction);
                    await Task.Delay(1000);
                }

                return;

                var needToBuy = 100 - myAuctions.Count + soldAuctions.Count;

                Console.WriteLine($"Need To Buy: {needToBuy}");

                var numberOfBoughtPlayers = 0;

                for (var index = 0; index < needToBuy;)
                {
                    Console.WriteLine($"Buying Players: {index + 1}/{needToBuy}");

                    var doWork = true;
                    var iteration = 0;

                    var numberOfBoughtPlayersInSession = 0;

                    while (doWork && iteration < 15)
                    {
                        await Task.Delay(15000);

                        addMinBid = !addMinBid;

                        iteration++;

                        Console.WriteLine($"Iteration: {iteration}");

                        var minBid = iteration % 3 == 0 ? 150 : iteration % 3 == 1 ? 200 : 250;

                        var auctions = await futService.GetCriteriaAvailableAuctions(minBid: minBid);

                        if (auctions == null || auctions.Count == 0)
                        {
                            Console.WriteLine("Not found");
                            continue;
                        }

                        foreach (var tradeAuction in auctions.OrderByDescending(a => a.Expires))
                        {
                            if (completed) break;

                            var auction = await futService.BuyOrBidAuction(tradeAuction.TradeId, tradeAuction.BuyNowPrice);

                            if (auction == null)
                            {
                                break;
                            }

                            numberOfBoughtPlayersInSession++;
                            numberOfBoughtPlayers++;
                            doWork = false;

                            Console.WriteLine($"Bought for {tradeAuction.BuyNowPrice}!!!");

                            await Task.Delay(1000);
                            var availableAuctions =
                                await futService.GetPlayerAvailableAuctions(auction.ItemData.AssetId);

                            await Task.Delay(1000);

                            if (availableAuctions.Count == 0)
                            {
                                Console.WriteLine("Good Catch!!!");
                                await futService.ListPlayer(auction.ItemData.Id, 1000);
                            }
                            else
                            {
                                Console.WriteLine(
                                    $"Available Count: {availableAuctions.Count}, Min Price: {availableAuctions.Min(a => a.BuyNowPrice)}");

                                var bestPrice = availableAuctions.Min(a => a.BuyNowPrice);
                                var sellPrice = bestPrice > 1000 ? bestPrice - 100 : bestPrice - 50;

                                await futService.ListPlayer(auction.ItemData.Id, Math.Max(sellPrice, 300));
                            }

                            if (numberOfBoughtPlayers == needToBuy)
                            {
                                completed = true;
                            }
                        }
                    }

                    if (completed) break;

                    if (doWork) continue;

                    index += numberOfBoughtPlayersInSession;
                }

                Console.WriteLine("Round Completed");

                await Task.Delay(1000 * 60);
            }
        }
    }
}