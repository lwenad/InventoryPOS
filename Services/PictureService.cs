using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace InventoryPOS.Services
{
    /// <summary>
    /// Provides filesystem-based picture lookup and thumbnail loading for inventory
    /// items, keyed by SKU. Pictures are stored at
    /// <c>{PictureFolderPath}\pictures\{SKU}\</c> and this class centralizes the
    /// folder-resolution and thumbnail-generation logic so it can be reused from
    /// both the edit form and the main grid.
    /// </summary>
    public static class PictureService
    {
        /// <summary>
        /// Supported image extensions (lowercase, including the dot).
        /// Matches the existing set used in <see cref="InventoryEditForm"/>.
        /// </summary>
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        /// <summary>
        /// Maximum number of pictures per SKU (mirrors the edit-form limit).
        /// </summary>
        public const int MaxPicturesPerSku = 20;

        /// <summary>
        /// Default thumbnail size for grid display.
        /// </summary>
        public const int DefaultThumbnailSize = 60;

        /// <summary>
        /// Builds the per-SKU picture folder path: <c>{pictureFolderPath}\pictures\{sku}</c>.
        /// </summary>
        /// <param name="pictureFolderPath">The configured root picture folder (may be null/empty).</param>
        /// <param name="sku">The item SKU used as the subfolder name.</param>
        /// <returns>The full path to the SKU picture folder, or <c>null</c> if the root folder is not configured.</returns>
        public static string? GetSkuPictureFolder(string? pictureFolderPath, string? sku)
        {
            if (string.IsNullOrWhiteSpace(pictureFolderPath) || string.IsNullOrWhiteSpace(sku))
                return null;
            return Path.Combine(pictureFolderPath, "pictures", sku);
        }

        /// <summary>
        /// Returns the full path of the first image file in the SKU's picture folder.
        /// </summary>
        /// <param name="pictureFolderPath">The configured root picture folder.</param>
        /// <param name="sku">The item SKU.</param>
        /// <returns>The full path to the first image, or <c>null</c> if no images found.</returns>
        public static string? GetFirstPicturePath(string? pictureFolderPath, string? sku)
        {
            var folder = GetSkuPictureFolder(pictureFolderPath, sku);
            if (folder == null || !Directory.Exists(folder))
                return null;

            var files = Directory.EnumerateFiles(folder)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .ToList();

            return files.FirstOrDefault();
        }

        /// <summary>
        /// Loads and scales an image from disk into a thumbnail bitmap.
        /// Uses a file stream + <see cref="Image.FromStream"/> pattern to avoid
        /// the file-locking behavior of <c>Image.FromFile</c>.
        /// </summary>
        /// <param name="imagePath">Full path to the source image file.</param>
        /// <param name="size">Desired width and height of the thumbnail.</param>
        /// <returns>A scaled <see cref="Bitmap"/>, or <c>null</c> on failure.</returns>
        public static Image? LoadThumbnail(string? imagePath, int size = DefaultThumbnailSize)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return null;

            try
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var source = Image.FromStream(stream);
                var thumb = new Bitmap(size, size);
                using (var graphics = Graphics.FromImage(thumb))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.DrawImage(source, 0, 0, size, size);
                }
                return thumb;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a cached placeholder image shown when an item has no pictures
        /// (or the folder is not configured). The image is a subtle gray rectangle.
        /// </summary>
        /// <param name="size">Desired width and height.</param>
        /// <returns>A gray placeholder <see cref="Bitmap"/>.</returns>
        public static Image GetPlaceholderImage(int size = DefaultThumbnailSize)
        {
            var placeholder = new Bitmap(size, size);
            using (var graphics = Graphics.FromImage(placeholder))
            {
                using var brush = new SolidBrush(Color.LightGray);
                using var pen = new Pen(Color.FromArgb(200, 200, 200));
                graphics.FillRectangle(brush, 0, 0, size, size);
                graphics.DrawRectangle(pen, 1, 1, size - 2, size - 2);
            }
            return placeholder;
        }
    }
}
