.using System.Text.Json.Serialization;

namespace InventoryPOS.Models
{
    public class UiState
    {
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; } // "Ascending" | "Descending" | null
        public string? FilterColumn { get; set; }
        public string? FilterValue { get; set; }
        public string? LastFilePath { get; set; }
        public string? PictureFolderPath { get; set; }
    }
}
