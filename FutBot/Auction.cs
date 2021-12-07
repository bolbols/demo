namespace FutBot
{
    public class Auction
    {
        public int BuyNowPrice { get; set; }
        public int CurrentBid { get; set; }
        public int StartingBid { get; set; }
        public int SuggestedBid => CurrentBid != 0 ? CurrentBid + 50 : StartingBid; 
        public long TradeId { get; set; }
        public long Expires { get; set; }
        public PlayerData ItemData { get; set; }
        public string BidState { get; set; }

        public string TradeState { get; set; }
    }
}