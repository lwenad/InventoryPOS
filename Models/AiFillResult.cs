using System.Text.Json.Serialization;

namespace InventoryPOS.Models
{
    /// <summary>
    /// Represents the structured fields that Google AI (Gemini) returns
    /// when invoked by the "AI Fill" feature in <see cref="Forms.InventoryEditForm"/>.
    /// </summary>
    public class AiFillResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("subCategory")]
        public string? SubCategory { get; set; }
    }
}
