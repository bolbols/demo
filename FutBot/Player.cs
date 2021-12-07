using System;

namespace FutBot
{
    public class Player
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public int Rating { get; set; }
        public int InSales { get; set; }
        public int Available { get; set; }
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public int AveragePrice { get; set; }
        public int MaxBuyPrice { get; set; }
        public int TotalSold { get; set; }
        public int SellPrice { get; set; }
        public bool Deprecated { get; set; }
        public int TotalSoldAllTime { get; set; }

        public DateTime LastSellDate { get; set; }

        public Player(long id, string name, string position, int rating, int maxBuyPrice = 300, int sellPrice = 0,
            bool deprecated = false)
        {
            Id = id;
            Name = name;
            Position = position;
            Rating = rating;
            MaxBuyPrice = maxBuyPrice;
            TotalSold = 0;
            SellPrice = sellPrice;
            Deprecated = deprecated;
            LastSellDate = DateTime.MinValue;
        }

        public Player()
        {
            
        }

        public override string ToString()
        {
            return $"{Name}|{InSales}|{Available}|{MinPrice}|{MaxPrice}|{AveragePrice}";
        }
    }
}