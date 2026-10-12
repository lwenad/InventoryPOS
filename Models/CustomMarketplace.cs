namespace InventoryPOS.Models
{
    public class CustomMarketplace
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public override string ToString() => $"{Name} — {Url}";
    }
}