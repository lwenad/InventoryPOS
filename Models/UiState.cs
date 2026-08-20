using System;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        public string? LogFolderPath { get; set; }
        // Default listing platforms for new items (comma-separated, e.g. "eBay, Poshmark")
        // Matches format of InventoryItem.ListingPlatform
        public string? DefaultListingPlatforms { get; set; }
        public int MaxImagesPerSku { get; set; } = 20;
        public bool ConfirmBeforeDelete { get; set; } = true;
        public List<string>? HiddenColumns { get; set; }
    }
}
