namespace FutBot
{
    public class FutBinPlayer
    {
        public string FutBinId { get; set; }
        public string FifaId { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public int Rating { get; set; }
        
        public int Count{ get; set; }

        public override string ToString() => $"{Name} | {Position} | {Rating}";
    }
}