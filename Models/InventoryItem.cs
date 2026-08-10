using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InventoryPOS.Models
{
    public class InventoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(80)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string SubCategory { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        public string Size { get; set; } = string.Empty;

        public string Condition { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string Colors { get; set; } = string.Empty;

        public decimal ListingPrice { get; set; } = 0m;

        public decimal COG { get; set; } = 0m;

        public string SKU { get; set; } = string.Empty;

        public string ListingPlatform { get; set; } = string.Empty;

        // Status of the item (e.g., Created, Sold)
        public string Status { get; set; } = "Created";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Price at which the item was sold (if applicable)
        public decimal SoldPrice { get; set; } = 0m;
        // Date when the item was sold (date-only). Null when not sold.
        public DateTime? SoldDate { get; set; } = null;
        // Earnings (selling revenue) for sold item
        public decimal Earnings { get; set; } = 0m;

        // Profit = Earnings - COG when sold, otherwise 0
        public decimal Profit => string.Equals(Status, "Sold", StringComparison.OrdinalIgnoreCase) ? (Earnings - COG) : 0m;
    }
}