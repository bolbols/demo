namespace FutBot
{
    public class Auction
    {
        public int BuyNowPrice { get; set; }
        public int CurrentBid { get; set; }
        public long TradeId { get; set; }
        public long Expires { get; set; }
        public PlayerData ItemData { get; set; }

        public string TradeState { get; set; }
    }
}