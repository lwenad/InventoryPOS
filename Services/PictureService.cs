using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp.Formats.Png;

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
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

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
        /// Returns all supported image file paths in the SKU's picture folder,
        /// sorted by file name so the display order is deterministic.
        /// </summary>
        /// <param name="pictureFolderPath">The configured root picture folder.</param>
        /// <param name="sku">The item SKU.</param>
        /// <returns>A list of full image paths (possibly empty) for the SKU.</returns>
        public static List<string> GetPicturePaths(string? pictureFolderPath, string? sku)
        {
            var folder = GetSkuPictureFolder(pictureFolderPath, sku);
            if (folder == null || !Directory.Exists(folder))
                return new List<string>();

            try
            {
                var files = Directory.GetFiles(folder)
                    .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                    .ToList();

                return files;
            }
            catch (DirectoryNotFoundException)
            {
                return new List<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Returns the full path of the first image file in the SKU's picture folder.
        /// </summary>
        /// <param name="pictureFolderPath">The configured root picture folder.</param>
        /// <param name="sku">The item SKU.</param>
        /// <returns>The full path to the first image, or <c>null</c> if no images found.</returns>
        public static string? GetFirstPicturePath(string? pictureFolderPath, string? sku)
        {
            return GetPicturePaths(pictureFolderPath, sku).FirstOrDefault();
        }

        /// <summary>
        /// Loads an image from disk as a <see cref="Bitmap"/>. Uses ImageSharp for WebP
        /// (which System.Drawing cannot decode) and falls back to System.Drawing for
        /// all other supported formats.
        /// </summary>
        /// <param name="imagePath">Full path to the source image file.</param>
        /// <returns>A <see cref="Bitmap"/> independent of any file or stream handles.</returns>
        private static Bitmap LoadImageBitmap(string imagePath)
        {
            var ext = Path.GetExtension(imagePath).ToLowerInvariant();

            if (ext == ".webp")
            {
                // ImageSharp decodes WebP; re-encode to PNG in a memory stream so
                // System.Drawing can consume it.
                using var image = SixLabors.ImageSharp.Image.Load(imagePath);
                using var ms = new MemoryStream();
                image.Save(ms, new PngEncoder());
                ms.Position = 0;
                using var temp = Image.FromStream(ms);
                return new Bitmap(temp); // clone so the stream can be safely disposed
            }

            // Standard image formats handled by System.Drawing
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }

        /// <summary>
        /// Loads and scales an image from disk into a thumbnail bitmap.
        /// Uses ImageSharp for WebP (which System.Drawing cannot decode) and
        /// falls back to System.Drawing for all other formats. The file stream
        /// is opened with <c>FileShare.ReadWrite</code> to avoid locking.
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
                using var source = LoadImageBitmap(imagePath);
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
        /// Loads a full-size image from disk, decoded at its native resolution.
        /// Uses ImageSharp for WebP (which System.Drawing cannot decode) and
        /// falls back to System.Drawing for all other supported formats. The
        /// returned bitmap is independent of any file or stream handle, so the
        /// source file is not locked after loading.
        /// </summary>
        /// <param name="imagePath">Full path to the source image file.</param>
        /// <returns>A full-size <see cref="Bitmap"/>, or <c>null</c> on failure.</returns>
        public static Image? LoadImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return null;

            try
            {
                return LoadImageBitmap(imagePath);
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
