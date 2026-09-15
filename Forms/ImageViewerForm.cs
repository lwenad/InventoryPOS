using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using InventoryPOS.Services;

namespace InventoryPOS.Forms
{
    /// <summary>
    /// Displays a full-size inventory picture, with arrow-key and button
    /// navigation across all pictures for the item's SKU. The image is scaled
    /// to fit the window (preserving aspect ratio) and a dark background keeps
    /// the focus on the picture. Navigation controls are hidden when the SKU
    /// has only a single picture.
    /// </summary>
    public class ImageViewerForm : Form
    {
        private readonly LoggerService _logger;
        private readonly List<string> _imagePaths;
        private PictureBox _pictureBox = null!;
        private Label _lblCounter = null!;
        private Button _btnPrev = null!;
        private Button _btnNext = null!;
        private int _currentIndex;
        private Image? _currentImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerForm"/>.
        /// </summary>
        /// <param name="imagePaths">All image paths for the SKU, in display order.</param>
        /// <param name="startIndex">Index of the image to display first.</param>
        public ImageViewerForm(IEnumerable<string> imagePaths, int startIndex = 0)
        {
            _logger = LoggerService.Instance;
            _imagePaths = (imagePaths ?? Enumerable.Empty<string>()).ToList();
            _currentIndex = _imagePaths.Count > 0
                ? Math.Clamp(startIndex, 0, _imagePaths.Count - 1)
                : 0;

            InitializeComponent();
            LoadCurrentImage();
            UpdateCounter();

            _logger.LogInfo($"ImageViewerForm opened: {_imagePaths.Count} picture(s), showing index {_currentIndex}.");
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Image Viewer";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(900, 700);
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.Black;
            this.KeyPreview = true;
            this.KeyDown += ImageViewer_KeyDown;
            this.FormClosed += ImageViewer_FormClosed;

            // Top control panel (counter + navigation + close)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            _lblCounter = new Label
            {
                Text = "0 / 0",
                ForeColor = Color.WhiteSmoke,
                AutoSize = true,
                Location = new Point(14, 14),
                Font = new Font("Segoe UI", 9F)
            };
            topPanel.Controls.Add(_lblCounter);

            _btnPrev = new Button
            {
                Text = "Prev",
                Size = new Size(64, 28),
                Location = new Point(100, 10),
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70)
            };
            _btnPrev.FlatAppearance.BorderSize = 0;
            _btnPrev.Click += (s, e) => Navigate(-1);
            topPanel.Controls.Add(_btnPrev);

            _btnNext = new Button
            {
                Text = "Next",
                Size = new Size(64, 28),
                Location = new Point(170, 10),
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70)
            };
            _btnNext.FlatAppearance.BorderSize = 0;
            _btnNext.Click += (s, e) => Navigate(1);
            topPanel.Controls.Add(_btnNext);

            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(32, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 60, 60)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            topPanel.Controls.Add(btnClose);

            this.Controls.Add(topPanel);

            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            this.Controls.Add(_pictureBox);

            // Hide navigation when there is nothing to page through.
            var showNav = _imagePaths.Count > 1;
            _btnPrev.Visible = showNav;
            _btnNext.Visible = showNav;

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Loads the full-size image for the current index, disposing the
        /// previously displayed image to avoid leaking GDI handles.
        /// </summary>
        private void LoadCurrentImage()
        {
            if (_currentImage != null)
            {
                _pictureBox.Image = null;
                _currentImage.Dispose();
                _currentImage = null;
            }

            if (_imagePaths.Count == 0)
            {
                _pictureBox.Image = null;
                _logger.LogInfo("ImageViewerForm: no images available to display.");
                return;
            }

            var path = _imagePaths[_currentIndex];
            var image = PictureService.LoadImage(path);
            if (image == null)
            {
                _logger.LogInfo($"ImageViewerForm: failed to load image at '{path}'.");
                return;
            }

            _currentImage = image;
            _pictureBox.Image = image;
        }

        private void UpdateCounter()
        {
            _lblCounter.Text = _imagePaths.Count == 0
                ? "No images"
                : $"{_currentIndex + 1} / {_imagePaths.Count}";
        }

        private void Navigate(int direction)
        {
            if (_imagePaths.Count == 0) return;
            _currentIndex = Math.Clamp(_currentIndex + direction, 0, _imagePaths.Count - 1);
            LoadCurrentImage();
            UpdateCounter();
        }

        private void ImageViewer_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                Navigate(1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                Navigate(-1);
                e.Handled = true;
            }
        }

        private void ImageViewer_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (_currentImage != null)
            {
                _pictureBox.Image = null;
                _currentImage.Dispose();
                _currentImage = null;
            }
        }
    }
}
