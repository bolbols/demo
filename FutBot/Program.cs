﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FutBot
{
    class Program
    {
        public static int Anomalies;

        static async Task Main(string[] args)
        {
            Constants.X_UT_SID = args[0];
            
            Console.ForegroundColor = ConsoleColor.White;
            /*var players = new Players();
            var options = new JsonSerializerOptions {WriteIndented = true};
            var jsonString = JsonSerializer.Serialize(players, options);
            await File.WriteAllTextAsync("players.json", jsonString);*/

            await SmartTargetingStrategy();
        }

        private static async Task SmartTargetingStrategy()
        {
            var totalSold = 0;

            var jsonString = await File.ReadAllTextAsync(@"players.json");
            var players = JsonSerializer.Deserialize<List<Player>>(jsonString);
            if (players == null) throw new NullReferenceException("Players");

            foreach (var player in players.Where(p => !p.Deprecated).OrderByDescending(p => p.TotalSoldAllTime))
            {
                Console.WriteLine(
                    $"Player: {player.Name}, Total Sold All Time: {player.TotalSoldAllTime}");
            }

            foreach (var player in players)
            {
                player.TotalSold = 0;
            }

            var error = false;

            while (true)
            {
                if (error) Environment.Exit(0);

                try
                {
                    var futService = new FutService();

                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine("New Round");

                    var auctions = await futService.GetAuctionsInTrade();

                    foreach (var player in players)
                    {
                        player.InSales = 0;
                    }

                    foreach (var auction in auctions.Where(a => a.Expires != -1 || a.TradeState != "closed"))
                    {
                        var player = players.FirstOrDefault(p => p.Id == auction.ItemData.AssetId);
                        if (player == null) continue;

                        player.InSales++;
                    }

                    await Task.Delay(1000);

                    var interestingPlayers = await DeleteSoldAuctions(auctions, players, futService);

                    await RelistExpiredAuctions(auctions, players, futService);

                    await SubmitIdleAuctions(auctions, players, futService);

                    var soldInCurrentSession = auctions.Count(a => a.Expires == -1 && a.TradeState == "closed");
                    totalSold += soldInCurrentSession;

                    var bought = 0;
                    var needToBuy = 100 - auctions.Count + soldInCurrentSession - Anomalies;

                    if (needToBuy > 0)
                    {
                        await BuyPlayers(needToBuy, players, interestingPlayers, bought, futService);
                    }


                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Bidding On Interesting Players");
                    await BidOnPlayers(players);

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    foreach (var player in players.Where(p => !p.Deprecated).OrderBy(p => p.InSales))
                    {
                        Console.WriteLine(
                            $"Player: {player.Name}, In Sales: {player.InSales}, Total Sold: {player.TotalSold}");
                    }

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{DateTime.Now} ==> Round Done, Total Sold: {totalSold}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    error = true;
                }
                finally
                {
                    var options = new JsonSerializerOptions {WriteIndented = true};
                    jsonString = JsonSerializer.Serialize(players.Where(p => !p.Deprecated || p.InSales > 0).ToList(),
                        options);
                    await File.WriteAllTextAsync("players.json", jsonString);
                    jsonString = JsonSerializer.Serialize(players.Where(p => p.Deprecated).ToList(), options);
                    await File.WriteAllTextAsync("deprecated-players.json", jsonString);
                }

                if (Anomalies > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"There Are {Anomalies} anomalies");
                }

                await Task.Delay(3 * 60 * 1000);
            }
        }

        private static async Task BidOnPlayers(List<Player> players)
        {
            var futService = new FutService();

            var watchList = await futService.GetAuctionsInWatchList();

            var auctionsToDelete = watchList.Where(a => a.Expires == -1 && a.BidState != "highest").ToList();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Deleting {auctionsToDelete.Count} Players Missed");

            foreach (var auction in auctionsToDelete)
            {
                await futService.DeleteWatchedAuction(auction.TradeId);
            }

            if (watchList.Count - auctionsToDelete.Count == 50) return;

            var possibleBids = 50 - watchList.Count + auctionsToDelete.Count;

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"Need to Bid On {possibleBids} Players");

            var playersBidOrder = players
                .Where(p => !p.Deprecated && watchList.Count(a => a.ItemData.AssetId == p.Id) < 5)
                .OrderBy(p => p.InSales).ThenBy(p => watchList.Count(a => a.ItemData.AssetId == p.Id)).ToList();

            foreach (var player in playersBidOrder)
            {
                if (possibleBids == 0) break;

                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine($"Bid On Player: {player.Name}");

                var maxBids = 5 - watchList.Count(a => a.ItemData.AssetId == player.Id);
                var bids = 0;

                var playerAuctions =
                    await futService.GetPlayerInBidRangeAuctions(player.Id, maxPrice: player.MaxBuyPrice - 50);

                if (playerAuctions.Count == 0) continue;

                var inTimeRangeAuctions =
                    playerAuctions.Where(a => a.Expires <= 600 && a.TradeState != "highest").ToList();

                if (inTimeRangeAuctions.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Player {player.Name} Not Available");
                    continue;
                }

                foreach (var auction in inTimeRangeAuctions.OrderBy(a => a.Expires))
                {
                    var bidAuction = await futService.BuyOrBidAuction(auction.TradeId, auction.SuggestedBid);

                    if (bidAuction != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Successful Bid On Player {player.Name} With {auction.SuggestedBid}");
                        possibleBids--;
                        bids++;

                        if (possibleBids == 0) break;
                        if (bids == maxBids) break;
                    }

                    await Task.Delay(1000);
                }

                await Task.Delay(5000);
            }
        }

        private static async Task BuyPlayers(int needToBuy, List<Player> players, List<long> interestingPlayers,
            int bought, FutService futService)
        {
            var purchasedPlayers = await futService.GetPurchasedPlayersList();

            if (purchasedPlayers.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"Need To List: {Math.Min(purchasedPlayers.Count, needToBuy - bought)}");

                foreach (var playerData in purchasedPlayers)
                {
                    if (bought == needToBuy) break;

                    var player = players.FirstOrDefault(p => p.Id == playerData.AssetId);

                    Console.WriteLine(player != null ? $"Listing Player: {player.Name}" : "Listing Unknown Player");

                    var sellPrice = await GetBestSellPrice(futService, player, playerData);

                    await futService.ListPlayer(playerData.Id, sellPrice, sellPrice >= 1000 ? 3600 : 10800);

                    bought++;
                }

                if (bought == needToBuy) return;
            }

            var watchList = await futService.GetAuctionsInWatchList();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Need To Buy: {needToBuy}");

            foreach (var player in players.Where(p => !p.Deprecated)
                .OrderByDescending(p => interestingPlayers.Contains(p.Id) ? 1 : 0)
                .ThenBy(p => p.InSales)
                .ThenByDescending(p => p.Rating))
            {
                if (bought == needToBuy) break;
                var maxPlayerToBuy = 5 - player.InSales;
                if (maxPlayerToBuy <= 0) continue;


                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(
                    $"Need To Buy {Math.Min(needToBuy - bought, maxPlayerToBuy)} For Player {player.Name}");

                var playerBought = 0;

                var bidAuctions = watchList.Where(a =>
                    a.Expires == -1 && a.BidState == "highest" && a.ItemData.AssetId == player.Id);

                foreach (var bidAuction in bidAuctions)
                {
                    if (playerBought == maxPlayerToBuy) break;
                    if (bought == needToBuy) break;

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Player {player.Name} Found In Watch List");

                    var sellPrice = await GetBestSellPrice(futService, player, bidAuction);

                    await futService.ListPlayer(bidAuction.ItemData.Id, sellPrice, sellPrice >= 1000 ? 3600 : 10800);

                    playerBought++;
                    bought++;
                }

                if (bought == needToBuy) break;

                var playerAuctions =
                    await futService.GetPlayerCheapAuctions(player.Id, maxPrice: player.MaxBuyPrice);

                if (playerAuctions.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Player {player.Name} Not Available");
                }

                foreach (var playerAuction in playerAuctions.OrderBy(a => a.BuyNowPrice))
                {
                    if (playerBought == maxPlayerToBuy) break;
                    if (bought == needToBuy) break;

                    var auction = await futService.BuyOrBidAuction(playerAuction.TradeId, playerAuction.BuyNowPrice);

                    if (auction == null)
                    {
                        break;
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Player {player.Name} Bought For {playerAuction.BuyNowPrice}");

                    var sellPrice = await GetBestSellPrice(futService, player, auction);

                    await futService.ListPlayer(auction.ItemData.Id, sellPrice, sellPrice >= 1000 ? 3600 : 10800);

                    playerBought++;
                    bought++;

                    await Task.Delay(1000);
                }

                await Task.Delay(3000);
            }
        }

        private static async Task<int> GetBestSellPrice(FutService futService, Player player, Auction auction)
        {
            int sellPrice;

            if (player.InSales == 0)
            {
                var cheapestAuctions = await futService.GetPlayerAvailableAuctions(player.Id);
                sellPrice = Math.Max(cheapestAuctions.Min(a => a.BuyNowPrice) - 50,
                    auction.ItemData.LastSalePrice + 300);

                if (sellPrice >= 1000 && (sellPrice % 100 != 0))
                {
                    sellPrice += 50;
                }
            }
            else
            {
                if (player.SellPrice != 0)
                {
                    sellPrice = player.SellPrice;
                }
                else
                {
                    sellPrice = auction.ItemData.LastSalePrice + 300;
                    if (sellPrice >= 1000 && (sellPrice % 100 != 0))
                    {
                        sellPrice += 50;
                    }
                }
            }

            if (!player.Deprecated)
            {
                sellPrice = Math.Max(sellPrice, 500);
            }

            if (player.SellPrice != 0)
            {
                sellPrice = Math.Max(sellPrice, player.SellPrice);
            }

            return sellPrice;
        }

        private static async Task<int> GetBestSellPrice(FutService futService, Player player, PlayerData playerData)
        {
            int sellPrice;

            if (player == null)
            {
                var cheapestAuctions = await futService.GetPlayerAvailableAuctions(playerData.AssetId);
                sellPrice = cheapestAuctions.Min(a => a.BuyNowPrice) - 50;
            }
            else
            {
                if (player.InSales == 0 || player.Id == 226518)
                {
                    var cheapestAuctions = await futService.GetPlayerAvailableAuctions(player.Id);
                    sellPrice = Math.Max(cheapestAuctions.Min(a => a.BuyNowPrice) - 50,
                        playerData.LastSalePrice + 300);

                    if (sellPrice >= 1000 && (sellPrice % 100 != 0))
                    {
                        sellPrice += 50;
                    }
                }
                else
                {
                    if (player.SellPrice != 0)
                    {
                        sellPrice = player.SellPrice;
                    }
                    else
                    {
                        sellPrice = playerData.LastSalePrice + 300;
                        if (sellPrice >= 1000 && (sellPrice % 100 != 0))
                        {
                            sellPrice += 50;
                        }
                    }
                }
            }

            sellPrice = Math.Max(sellPrice, 500);

            if (player != null && player.SellPrice != 0)
            {
                sellPrice = Math.Max(sellPrice, player.SellPrice);
            }

            return sellPrice;
        }

        private static async Task<List<long>> DeleteSoldAuctions(List<Auction> auctions, List<Player> players,
            FutService futService)
        {
            var interestingPlayers = new List<long>();

            var soldAuctions = auctions.Where(a => a.Expires == -1 && a.TradeState == "closed").ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Need To Delete: {soldAuctions.Count}");

            foreach (var auction in soldAuctions)
            {
                var player = players.FirstOrDefault(p => p.Id == auction.ItemData.AssetId);

                if (player is not null)
                {
                    if (auction.CurrentBid >= 1000 && !interestingPlayers.Contains(player.Id))
                    {
                        interestingPlayers.Add(player.Id);
                    }

                    player.TotalSold++;
                    player.TotalSoldAllTime++;
                    player.LastSellDate = DateTime.Now;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Deleting Player {player.Name} Sold With {auction.CurrentBid}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Deleting Unknown Player Sold With {auction.CurrentBid}");
                }

                await futService.DeleteSoldAuction(auction.TradeId);
                await Task.Delay(2000);
            }

            return interestingPlayers;
        }

        private static async Task RelistExpiredAuctions(List<Auction> auctions, List<Player> players,
            FutService futService)
        {
            var expiredAuctions = auctions.Where(a =>
                a.Expires == -1 && a.TradeState == "expired").ToList();

            if (expiredAuctions.Count == 0) return;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Need To Resubmit: {expiredAuctions.Count}");

            foreach (var auction in expiredAuctions.Take(5))
            {
                int sellPrice;
                var player = players.FirstOrDefault(p => p.Id == auction.ItemData.AssetId);

                Console.WriteLine(player != null ? $"Resubmit Player {player.Name}" : "Resubmit Unknown Player");


                if (player is not null && player.SellPrice != 0)
                {
                    sellPrice = player.SellPrice;
                }
                else
                {
                    if (player is {Deprecated: false} && auction.BuyNowPrice >= 1000)
                    {
                        sellPrice = await GetBestSellPrice(futService, player, auction);
                    }
                    else
                    {
                        sellPrice = auction.ItemData.LastSalePrice + 300;
                        if (sellPrice >= 1000 && (sellPrice % 100 != 0))
                        {
                            sellPrice += 50;
                        }

                        if (player is {Deprecated: false})
                        {
                            sellPrice = Math.Max(sellPrice, 500);
                        }
                    }
                }

                await futService.SellPlayer(auction.ItemData.Id, sellPrice, sellPrice >= 1000 ? 3600 : 10800);
                await Task.Delay(5000);
            }

            //await futService.ResubmitList();
        }

        private static async Task SubmitIdleAuctions(List<Auction> auctions, List<Player> players,
            FutService futService)
        {
            var idleAuctions = auctions.Where(a => a.Expires == 0).ToList();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Need To Submit: {idleAuctions.Count}");

            foreach (var auction in idleAuctions)
            {
                int sellPrice;
                var player = players.FirstOrDefault(p => p.Id == auction.ItemData.AssetId);

                if (player is not null && player.SellPrice != 0)
                {
                    sellPrice = player.SellPrice;
                }
                else
                {
                    sellPrice = auction.ItemData.LastSalePrice + 300;
                    if (sellPrice >= 1000 && (sellPrice % 100 != 0))
                    {
                        sellPrice += 50;
                    }
                }

                await futService.SellPlayer(auction.ItemData.Id, sellPrice, sellPrice >= 1000 ? 3600 : 10800);
                await Task.Delay(1000);

                if (Anomalies > 0) Anomalies--;
            }
        }
    }
}