using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq; // Required for the .Cast<string>() extension
using System.Windows.Forms;
using InventoryPOS.Models;

namespace InventoryPOS.Forms
{
    public partial class InventoryEditForm : Form
    {
        private readonly InventoryItem _item;
        private readonly bool _isNew;

        private Panel mainPanel = null!;
        private TextBox txtTitle = null!;
        private TextBox txtDescription = null!;
        private TextBox txtCategory = null!;
        private TextBox txtSubCategory = null!;
        private NumericUpDown numQuantity = null!;
        private ComboBox cmbSize = null!;
        private TextBox txtCustomSize = null!;
        private TextBox txtBrand = null!;
        private TextBox txtColors = null!;
        private NumericUpDown numListingPrice = null!;
        private NumericUpDown numCOG = null!;
        private TextBox txtSKU = null!;
        private CheckedListBox chkPlatform = null!; // Changed to CheckedListBox
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private ComboBox cmbCondition = null!;
        private ComboBox cmbStatus = null!;
        private NumericUpDown numSoldPrice = null!;
        private DateTimePicker dtSoldDate = null!;
        private NumericUpDown numEarnings = null!;

        // Fields for picture management tab
        private UiState? _uiState;
        private TabControl? pictureManagementTabControl;
        private FlowLayoutPanel? _flowLayoutPanel;
        private Panel? _middlePanel;
        private Panel? _picturePanel;
        private Label? _lblDropZone;
        private Button? _btnAddPicture;
        private Button? _btnRemovePicture;
        private Button? _btnClearAll;
        private string? _selectedPicturePath;

        public InventoryItem ResultItem { get; private set; } = null!;

        public InventoryEditForm(InventoryItem? item = null)
        {
            _item = item ?? new InventoryItem();
            _isNew = item == null;

            // Load UI state synchronously so the Picture Management tab is
            // enabled correctly from the start (no race with async load)
            _uiState = new InventoryPOS.Services.InventoryRepository().LoadUiState()
                       ?? new UiState();

            // 1. Initialize strictly standard designer properties
            InitializeComponent();

            // 2. Set dynamic properties and custom UI
            this.Text = _isNew ? "Add Inventory Item" : "Edit Inventory Item";
            SetupCustomUI();

            // 3. Populate data
            LoadItemData();
        }

        private void PictureManagementTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // When the user navigates to the Picture Management tab, refresh its
            // content so it reflects the current _uiState (e.g. folder was set after
            // the form was opened, or pictures were added/removed)
            if (pictureManagementTabControl?.SelectedTab?.Text == "Picture Management")
            {
                RefreshPictureManagementTab();
            }
        }

        private void RefreshPictureManagementTab()
        {
            var tabPictures = pictureManagementTabControl?.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "Picture Management");
            if (tabPictures == null) return;

            // Clear existing controls
            tabPictures.Controls.Clear();

            // Re-setup with loaded UI state
            SetupPictureManagementUI(tabPictures);
        }

        /// <summary>
        /// Strictly static assignments only. No ternary expressions, loops, or custom calls here.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(620, 660); // Increased width to fit two-panel layout
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Inventory Item";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupPictureManagementUI(TabPage tabPage)
        {
            // Check if picture folder is configured
            if (string.IsNullOrWhiteSpace(_uiState?.PictureFolderPath))
            {
                // Display a message indicating that picture management is disabled
                var lblDisabled = new Label
                {
                    Text = "Picture management is disabled. Please configure the picture folder in Application Configuration.",
                    Location = new Point(20, 20),
                    Size = new Size(340, 40),
                    ForeColor = Color.Gray,
                    AutoSize = true
                };
                tabPage.Controls.Add(lblDisabled);
                return;
            }

            // Validate that the configured folder exists
            if (!Directory.Exists(_uiState.PictureFolderPath))
            {
                var lblInvalidFolder = new Label
                {
                    Text = $"Configured picture folder does not exist: {_uiState.PictureFolderPath}",
                    Location = new Point(20, 20),
                    Size = new Size(340, 40),
                    ForeColor = Color.Red,
                    AutoSize = true
                };
                tabPage.Controls.Add(lblInvalidFolder);
                return;
            }

            // Main container
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(8)
            };
            tabPage.Controls.Add(mainContainer);

            // --- TOP: Three buttons centered on same level ---
            var btnPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(580, 40),
                BorderStyle = BorderStyle.None,
                BackColor = Color.Transparent
            };
            // Add Picture button
            var btnAddPicture = new Button
            {
                Text = "Add Picture",
                Location = new Point(20, 0),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
            };
            btnAddPicture.FlatAppearance.BorderSize = 1;
            btnAddPicture.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            btnAddPicture.MouseEnter += (s, e) => { btnAddPicture.BackColor = Color.FromArgb(230, 245, 255); btnAddPicture.ForeColor = Color.FromArgb(0, 122, 204); };
            btnAddPicture.MouseLeave += (s, e) => { btnAddPicture.BackColor = Color.FromArgb(0, 122, 204); btnAddPicture.ForeColor = Color.White; };
            btnAddPicture.Click += BtnAddPicture_Click;
            btnPanel.Controls.Add(btnAddPicture);
            // Remove Selected button
            var btnRemovePicture = new Button
            {
                Text = "Remove Selected",
                Location = new Point(155, 0),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnRemovePicture.FlatAppearance.BorderSize = 1;
            btnRemovePicture.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnRemovePicture.MouseEnter += (s, e) => { btnRemovePicture.BackColor = Color.FromArgb(240, 240, 240); };
            btnRemovePicture.MouseLeave += (s, e) => { btnRemovePicture.BackColor = Color.White; };
            btnRemovePicture.Click += BtnRemovePicture_Click;
            btnPanel.Controls.Add(btnRemovePicture);
            // Clear All button
            var btnClearAll = new Button
            {
                Text = "Clear All",
                Location = new Point(290, 0),
                Size = new Size(120, 30),
                ForeColor = Color.FromArgb(180, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnClearAll.FlatAppearance.BorderSize = 1;
            btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnClearAll.MouseEnter += (s, e) => { btnClearAll.BackColor = Color.FromArgb(255, 230, 230); };
            btnClearAll.MouseLeave += (s, e) => { btnClearAll.BackColor = Color.White; btnClearAll.ForeColor = Color.FromArgb(180, 50, 50); };
            btnClearAll.Click += BtnClearAllPictures_Click;
            btnPanel.Controls.Add(btnClearAll);
            mainContainer.Controls.Add(btnPanel);

            // Middle panel - shows "No pictures found" message when there are no pictures
            _middlePanel = new Panel
            {
                Location = new Point(10, 50),
                Size = new Size(580, 30),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245),
                Visible = false
            };
            mainContainer.Controls.Add(_middlePanel);

            // --- BOTTOM: Picture thumbnails vertically aligned ---
            // Moved down to accommodate the middle panel above it
            var bottomPanel = new Panel
            {
                Location = new Point(10, 80),
                Size = new Size(580, 420),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            mainContainer.Controls.Add(bottomPanel);

            // Header for picture area
            var lblPicHeader = new Label
            {
                Text = "Pictures:",
                Location = new Point(12, 8),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            bottomPanel.Controls.Add(lblPicHeader);

            // Flow layout for thumbnail pictures
            var thumbFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            bottomPanel.Controls.Add(thumbFlow);

            // Store reference for LoadPictures
            _flowLayoutPanel = thumbFlow;

            // Load existing pictures
            LoadPictures();
        }

        private void SetupCustomUI()
        {
            this.SuspendLayout();

            // TabControl hosts both the inventory editor and the (future) picture management page
            pictureManagementTabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            pictureManagementTabControl.Name = "tabControl";

            // Tab 1 — Inventory Edit: keeps all the existing editor logic and controls.
            // mainPanel is the scrollable container the helpers and CreateControls() target.
            var tabInventory = new TabPage("Inventory Edit");

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            tabInventory.Controls.Add(mainPanel);

            // Tab 2 — Picture Management: placeholder, empty for now
            var tabPictures = new TabPage("Picture Management");

            pictureManagementTabControl.TabPages.Add(tabInventory);
            pictureManagementTabControl.TabPages.Add(tabPictures);
            this.Controls.Add(pictureManagementTabControl);

            // Refresh the Picture Management tab when the user navigates to it,
            // so it reflects the latest configuration (folder location) state
            pictureManagementTabControl.SelectedIndexChanged += PictureManagementTabControl_SelectedIndexChanged;

            CreateControls();

            this.ResumeLayout(true);
        }

        private void CreateControls()
        {
            var labelWidth = 100;
            var controlWidth = 350;
            var startY = 20;
            var spacing = 35;
            var y = startY;

            // Title
            AddLabel("Title *", 20, y, labelWidth);
            txtTitle = AddTextBox(130, y, controlWidth);
            txtTitle.MaxLength = 80;
            var lblTitleCount = AddLabel("0/80", 490, y, 50);
            lblTitleCount.ForeColor = Color.Gray;
            txtTitle.TextChanged += (s, e) => lblTitleCount.Text = $"{txtTitle.Text.Length}/80";
            y += spacing;

            // Description
            AddLabel("Description", 20, y, labelWidth);
            txtDescription = AddTextBox(130, y, controlWidth, 60);
            txtDescription.Multiline = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            y += spacing + 30;

            // Category
            AddLabel("Category", 20, y, labelWidth);
            txtCategory = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Sub Category
            AddLabel("Sub Category", 20, y, labelWidth);
            txtSubCategory = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Quantity
            AddLabel("Quantity", 20, y, labelWidth);
            numQuantity = AddNumericUpDown(130, y, 100, 0, 999999);
            y += spacing;

            // Size (predefined list with Custom option)
            AddLabel("Size", 20, y, labelWidth);
            cmbSize = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSize.Items.AddRange(new[] { "XXS", "XS", "S", "M", "L", "XL", "2XL", "3XL", "4XL", "5XL", "Custom" });
            cmbSize.SelectedIndexChanged += (s, e) =>
            {
                var isCustom = string.Equals(cmbSize.SelectedItem?.ToString(), "Custom", StringComparison.OrdinalIgnoreCase);
                txtCustomSize.Visible = isCustom;
                txtCustomSize.Enabled = isCustom;
                if (isCustom)
                    txtCustomSize.Focus();
            };
            mainPanel.Controls.Add(cmbSize);

            // Custom size textbox (hidden by default)
            txtCustomSize = AddTextBox(340, y, controlWidth - 210);
            txtCustomSize.Visible = false;
            txtCustomSize.Enabled = false;
            y += spacing;

            // Condition
            AddLabel("Condition", 20, y, labelWidth);
            cmbCondition = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCondition.Items.AddRange(new[]
            {
    "New with tags (NWT)",
    "Pre-owned - Excellent",
    "Pre-owned - Good",
    "Pre-owned - Fair"
});
            mainPanel.Controls.Add(cmbCondition);
            y += spacing;

            // (Status control moved below Earnings)

            // Brand
            AddLabel("Brand", 20, y, labelWidth);
            txtBrand = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Colors
            AddLabel("Colors", 20, y, labelWidth);
            txtColors = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Listing Price
            AddLabel("Listing Price", 20, y, labelWidth);
            numListingPrice = AddNumericUpDown(130, y, 150, 0, 999999, 2);
            y += spacing;

            // COG
            AddLabel("COG", 20, y, labelWidth);
            numCOG = AddNumericUpDown(130, y, 150, 0, 999999, 2);
            y += spacing;

            // Sold Price
            AddLabel("Sold Price", 20, y, labelWidth);
            var tempSold = AddNumericUpDown(130, y, 150, 0, 999999, 2);
            // keep a named field reference
            numSoldPrice = tempSold;
            numSoldPrice.DecimalPlaces = 2;
            numSoldPrice.Enabled = false; // default disabled until Status == Sold
            mainPanel.Controls.Add(numSoldPrice);
            y += spacing;

            // Earnings (revenue)
            AddLabel("Earnings", 20, y, labelWidth);
            var tempEarnings = AddNumericUpDown(130, y, 150, 0, 999999, 2);
            numEarnings = tempEarnings;
            numEarnings.DecimalPlaces = 2;
            numEarnings.Enabled = false; // enabled when Status == Sold
            mainPanel.Controls.Add(numEarnings);
            y += spacing;

            // Status (moved here so it appears after Earnings)
            AddLabel("Status", 20, y, labelWidth);
            cmbStatus = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatus.Items.AddRange(new[] { "Created", "Sold" });
            cmbStatus.SelectedIndex = 0; // default to Created
            cmbStatus.SelectedIndexChanged += CmbStatus_SelectedIndexChanged;
            mainPanel.Controls.Add(cmbStatus);
            y += spacing;

            // Sold Date
            AddLabel("Sold Date", 20, y, labelWidth);
            dtSoldDate = new DateTimePicker
            {
                Location = new Point(130, y),
                Size = new Size(200, 25),
                Format = DateTimePickerFormat.Short,
                ShowUpDown = false,
                Enabled = false
            };
            mainPanel.Controls.Add(dtSoldDate);
            y += spacing;

            // SKU
            AddLabel("SKU", 20, y, labelWidth);
            txtSKU = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Platform (Updated UI)
            AddLabel("Listing Platform", 20, y, labelWidth);
            chkPlatform = new CheckedListBox
            {
                Location = new Point(130, y),
                Size = new Size(controlWidth, 65),
                CheckOnClick = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            chkPlatform.Items.AddRange(new string[] { "eBay", "Poshmark", "Depop" });
            mainPanel.Controls.Add(chkPlatform);

            y += 80;

            // Action Buttons
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(280, y),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            mainPanel.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(380, y),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            mainPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;

            this.ResumeLayout(true);

            // Load picture management controls for tab 2
            if (pictureManagementTabControl?.TabPages.Count > 1)
            {
                var tabPictures = pictureManagementTabControl.TabPages[1];
                SetupPictureManagementUI(tabPictures);
            }
        }

        private Label AddLabel(string text, int x, int y, int width)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Size = new Size(width, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(label);
            return label;
        }

        private TextBox AddTextBox(int x, int y, int width, int height = 25)
        {
            var textBox = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height)
            };
            mainPanel.Controls.Add(textBox);
            return textBox;
        }

        private NumericUpDown AddNumericUpDown(int x, int y, int width, decimal min, decimal max, int decimalPlaces = 0)
        {
            var numeric = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimalPlaces,
                ThousandsSeparator = true
            };
            mainPanel.Controls.Add(numeric);
            return numeric;
        }

        private void LoadItemData()
        {
            txtTitle.Text = _item.Title;
            txtDescription.Text = _item.Description;
            txtCategory.Text = _item.Category;
            txtSubCategory.Text = _item.SubCategory;
            numQuantity.Value = Math.Min(Math.Max(_item.Quantity, numQuantity.Minimum), numQuantity.Maximum);
            // Load size: if it matches one of the combo items select it, otherwise select Custom and populate custom textbox
            if (!string.IsNullOrWhiteSpace(_item.Size) && cmbSize.Items.Contains(_item.Size))
            {
                cmbSize.SelectedItem = _item.Size;
                txtCustomSize.Text = string.Empty;
                txtCustomSize.Visible = false;
                txtCustomSize.Enabled = false;
            }
            else if (!string.IsNullOrWhiteSpace(_item.Size))
            {
                cmbSize.SelectedItem = "Custom";
                txtCustomSize.Text = _item.Size;
                txtCustomSize.Visible = true;
                txtCustomSize.Enabled = true;
            }
            else
            {
                cmbSize.SelectedIndex = -1;
                txtCustomSize.Text = string.Empty;
                txtCustomSize.Visible = false;
                txtCustomSize.Enabled = false;
            }

            // Set ComboBox selection for Condition
            if (!string.IsNullOrWhiteSpace(_item.Condition) && cmbCondition.Items.Contains(_item.Condition))
                cmbCondition.SelectedItem = _item.Condition;
            else if (_isNew)
                cmbCondition.SelectedItem = "Pre-owned - Excellent"; // default for new items
            else
                cmbCondition.SelectedIndex = -1;

            // Status
            if (!string.IsNullOrWhiteSpace(_item.Status) && cmbStatus.Items.Contains(_item.Status))
                cmbStatus.SelectedItem = _item.Status;
            else
                cmbStatus.SelectedIndex = 0; // default Created

            txtBrand.Text = _item.Brand;
            txtColors.Text = _item.Colors;
            numListingPrice.Value = Math.Min(Math.Max(_item.ListingPrice, numListingPrice.Minimum), numListingPrice.Maximum);
            numCOG.Value = Math.Min(Math.Max(_item.COG, numCOG.Minimum), numCOG.Maximum);
            txtSKU.Text = _item.SKU;

            // Platform multi-select load logic
            if (!string.IsNullOrWhiteSpace(_item.ListingPlatform))
            {
                var savedPlatforms = _item.ListingPlatform.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var platform in savedPlatforms)
                {
                    int index = chkPlatform.Items.IndexOf(platform.Trim());
                    if (index >= 0)
                    {
                        chkPlatform.SetItemChecked(index, true);
                    }
                }
            }
            else if (_isNew)
            {
                // Default platform for new items
                int poshIndex = chkPlatform.Items.IndexOf("Poshmark");
                if (poshIndex >= 0) chkPlatform.SetItemChecked(poshIndex, true);
            }

            // Load sold price and enable/disable controls based on status
            numSoldPrice.Value = Math.Min(Math.Max(_item.SoldPrice, numSoldPrice.Minimum), numSoldPrice.Maximum);
            // Load earnings
            numEarnings.Value = Math.Min(Math.Max(_item.Earnings, numEarnings.Minimum), numEarnings.Maximum);
            var isSold = string.Equals(cmbStatus.SelectedItem?.ToString(), "Sold", StringComparison.OrdinalIgnoreCase);
            // Ensure sold price and sold date controls reflect the current selected status
            numSoldPrice.Enabled = isSold;
            numEarnings.Enabled = isSold;
            if (_item.SoldDate.HasValue)
            {
                try { dtSoldDate.Value = _item.SoldDate.Value.Date; } catch { dtSoldDate.Value = DateTime.Today; }
            }
            else
            {
                dtSoldDate.Value = DateTime.Today;
            }
            dtSoldDate.Enabled = isSold;
        }

        private void CmbStatus_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var isSold = string.Equals(cmbStatus.SelectedItem?.ToString(), "Sold", StringComparison.OrdinalIgnoreCase);
            numSoldPrice.Enabled = isSold;
            // If status is Sold, enable earnings and sold date and set to today if not present
            numEarnings.Enabled = isSold;
            dtSoldDate.Enabled = isSold;
            if (isSold)
            {
                // If the current item didn't have a sold date, set it to today
                if (!_item.SoldDate.HasValue)
                {
                    try { dtSoldDate.Value = DateTime.Today; } catch { }
                }
            }
        }

private async void LoadPictures()
        {
            if (_flowLayoutPanel == null) return;

            _flowLayoutPanel.Controls.Clear();

            // Get the SKU folder path
            var skuFolder = Path.Combine(_uiState?.PictureFolderPath ?? string.Empty, "pictures", _item.SKU);

            // Hide middle panel initially (will show only when no pictures)
            _middlePanel?.Visible = false;
            _middlePanel?.Controls.Clear();

            bool folderExists = Directory.Exists(skuFolder);

            if (!folderExists)
            {
                // Show "No pictures found" in middle panel, don't display picture boxes
                _middlePanel?.Visible = true;
                _middlePanel?.Controls.Add(new Label
                {
                    Text = "No pictures found for this SKU.",
                    Location = new Point(20, 5),
                    Size = new Size(320, 20),
                    ForeColor = Color.Gray,
                    AutoSize = true
                });
                return;
            }

            var pictureFiles = Directory.GetFiles(skuFolder, "*.jpg")
                .Concat(Directory.GetFiles(skuFolder, "*.jpeg"))
                .Concat(Directory.GetFiles(skuFolder, "*.png"))
                .Concat(Directory.GetFiles(skuFolder, "*.gif"))
                .OrderBy(f => f)
                .Take(20)
                .ToList();

            if (!pictureFiles.Any())
            {
                // Show "No pictures found" in middle panel, don't display picture boxes
                _middlePanel?.Visible = true;
                _middlePanel?.Controls.Add(new Label
                {
                    Text = "No pictures found for this SKU.",
                    Location = new Point(20, 5),
                    Size = new Size(320, 20),
                    ForeColor = Color.Gray,
                    AutoSize = true
                });
                return;
            }

            // Display pictures as thumbnails
            var y = 10;
            foreach (var pictureFile in pictureFiles)
            {
                try
                {
                    // Create picture container
                    var picContainer = new Panel
                    {
                        Size = new Size(140, 160),
                        BorderStyle = BorderStyle.FixedSingle,
                        Margin = new Padding(5)
                    };

                    // Load thumbnail
                    using var image = Image.FromFile(pictureFile);
                    var thumbnail = GetThumbnail(image, 120, 120);

                    var picBox = new PictureBox
                    {
                        Image = thumbnail,
                        Size = new Size(120, 120),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Location = new Point(10, 10),
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    // Add filename label
                    var fileName = new Label
                    {
                        Text = Path.GetFileName(pictureFile),
                        Location = new Point(10, 135),
                        Size = new Size(120, 15),
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.Gray,
                        Font = new Font("Segoe UI", 7F)
                    };

                    picContainer.Controls.AddRange(new Control[] { picBox, fileName });
                    picContainer.Click += (s, e) => SelectPicture(picBox, pictureFile);
                    picBox.Click += (s, e) => SelectPicture(picBox, pictureFile);

                    picContainer.Top = y;
                    _flowLayoutPanel.Controls.Add(picContainer);

                    y += 165;
                    if (y > 200) // Allow scrolling
                    {
                        _flowLayoutPanel.AutoScroll = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading image {pictureFile}: {ex.Message}");
                }
            }
        }

        private Image GetThumbnail(Image sourceImage, int width, int height)
        {
            var thumb = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(thumb))
            {
                graphics.DrawImage(sourceImage, 0, 0, width, height);
            }
            return thumb;
        }

        private void SelectPicture(PictureBox pictureBox, string filePath)
        {
            // Clear previous selection
            if (_flowLayoutPanel == null) return;
            foreach (Control control in _flowLayoutPanel.Controls)
            {
                if (control is Panel panel)
                {
                    panel.BorderStyle = BorderStyle.FixedSingle;
                    foreach (Control child in panel.Controls)
                    {
                        if (child is PictureBox picBox)
                        {
                            picBox.BorderStyle = BorderStyle.FixedSingle;
                        }
                    }
                }
            }

            // Highlight selected picture
            var selectedPanel = pictureBox.Parent as Panel;
            if (selectedPanel != null)
            {
                selectedPanel.BorderStyle = BorderStyle.Fixed3D;
                if (pictureBox.Parent is Panel parent)
                {
                    foreach (Control child in parent.Controls)
                    {
                        if (child is PictureBox picBox)
                        {
                            picBox.BorderStyle = BorderStyle.Fixed3D;
                        }
                    }
                }
            }

            // Store selected picture path for actions
            _selectedPicturePath = filePath;
        }

        private void PictureDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data is null) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            bool hasImageFiles = files.Any(f => IsImageFile(f));
            if (_lblDropZone != null)
            {
                if (hasImageFiles)
                {
                    _lblDropZone.BackColor = Color.FromArgb(200, 255, 200);
                }
                else
                {
                    _lblDropZone.BackColor = Color.FromArgb(255, 200, 200);
                }
            }
        }

        private void PictureDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data is null) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            var imageFiles = files.Where(f => IsImageFile(f)).ToList();

            if (imageFiles.Any())
            {
                _lblDropZone!.BackColor = Color.FromArgb(240, 240, 240);
                AddPicturesToSKU(imageFiles);
            }
            else
            {
                MessageBox.Show("Please drop only image files (JPG, PNG, GIF).", "Invalid Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _lblDropZone!.BackColor = Color.FromArgb(255, 200, 200);
            }
        }

        private bool IsImageFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif";
        }

        private void BtnAddPicture_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select Images",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                AddPicturesToSKU(dialog.FileNames.ToList());
            }
        }

        private async void AddPicturesToSKU(List<string> imageFiles)
        {
            if (imageFiles == null || imageFiles.Count == 0)
                return;

            // Check if we've reached the limit of 20 pictures
            var skuFolder = Path.Combine(_uiState?.PictureFolderPath ?? string.Empty, "pictures", _item.SKU);
            int existingPictures = 0;
            if (Directory.Exists(skuFolder))
            {
                existingPictures = Directory.GetFiles(skuFolder, "*.jpg").Concat(Directory.GetFiles(skuFolder, "*.jpeg")).Concat(Directory.GetFiles(skuFolder, "*.png")).Concat(Directory.GetFiles(skuFolder, "*.gif")).Count();
            }

            if (existingPictures + imageFiles.Count > 20)
            {
                MessageBox.Show("You cannot exceed 20 pictures for this item.", "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Ensure the SKU folder exists
                Directory.CreateDirectory(skuFolder);

                // Copy images to SKU folder with unique names
                var copiedFiles = new List<string>();
                foreach (var sourceFile in imageFiles)
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var destinationFile = Path.Combine(skuFolder, fileName);

                    // If file exists, add a number to make it unique
                    if (File.Exists(destinationFile))
                    {
                        var counter = 1;
                        var baseName = Path.GetFileNameWithoutExtension(fileName);
                        var extension = Path.GetExtension(fileName);
                        do
                        {
                            var newFileName = $"{baseName}_{counter}{extension}";
                            destinationFile = Path.Combine(skuFolder, newFileName);
                            counter++;
                        } while (File.Exists(destinationFile));
                    }

                    File.Copy(sourceFile, destinationFile);
                    copiedFiles.Add(destinationFile);
                }

                // Refresh the picture display
                LoadPictures();

                MessageBox.Show($"Successfully added {copiedFiles.Count} picture(s) to SKU {_item.SKU}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add pictures: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRemovePicture_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedPicturePath))
            {
                MessageBox.Show("Please select a picture to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to remove the picture '{Path.GetFileName(_selectedPicturePath)}'?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    File.Delete(_selectedPicturePath);

                    // Refresh the picture display
                    LoadPictures();

                    MessageBox.Show("Picture removed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _selectedPicturePath = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to remove picture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnClearAllPictures_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to remove ALL pictures for this SKU? This cannot be undone.", "Confirm Clear All", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var skuFolder = Path.Combine(_uiState?.PictureFolderPath ?? string.Empty, "pictures", _item.SKU);
                    if (Directory.Exists(skuFolder))
                    {
                        foreach (var file in Directory.GetFiles(skuFolder))
                        {
                            File.Delete(file);
                        }
                        Directory.Delete(skuFolder);
                    }

                    // Refresh the picture display
                    LoadPictures();

                    MessageBox.Show("All pictures removed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to remove pictures: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Title is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }

            if (txtTitle.Text.Length > 80)
            {
                MessageBox.Show("Title cannot exceed 80 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }

            // Required fields
            if (string.IsNullOrWhiteSpace(txtCategory.Text))
            {
                MessageBox.Show("Category is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCategory.Focus();
                return;
            }

            // Size validation: either a predefined size must be selected or a custom size entered
            if (cmbSize.SelectedItem == null)
            {
                MessageBox.Show("Size is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbSize.Focus();
                return;
            }
            if (string.Equals(cmbSize.SelectedItem?.ToString(), "Custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(txtCustomSize.Text))
            {
                MessageBox.Show("Please enter a custom size.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCustomSize.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtBrand.Text))
            {
                MessageBox.Show("Brand is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBrand.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtColors.Text))
            {
                MessageBox.Show("Colors is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtColors.Focus();
                return;
            }

            // If status is Sold, ensure a sold price and earnings are provided (> 0)
            var selectedStatus = cmbStatus.SelectedItem?.ToString() ?? string.Empty;
            if (string.Equals(selectedStatus, "Sold", StringComparison.OrdinalIgnoreCase))
            {
                if (numSoldPrice.Value <= 0m)
                {
                    MessageBox.Show("Sold items must have a Sold Price greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numSoldPrice.Focus();
                    return;
                }
                if (numEarnings.Value <= 0m)
                {
                    MessageBox.Show("Sold items must have Earnings greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numEarnings.Focus();
                    return;
                }
            }

            ResultItem = new InventoryItem
            {
                Id = _item.Id,
                Title = txtTitle.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                SubCategory = txtSubCategory.Text.Trim(),
                Quantity = (int)numQuantity.Value,
                Size = string.Equals(cmbSize.SelectedItem?.ToString(), "Custom", StringComparison.OrdinalIgnoreCase)
                    ? txtCustomSize.Text.Trim()
                    : (cmbSize.SelectedItem?.ToString() ?? string.Empty),
                Condition = cmbCondition.SelectedItem?.ToString() ?? string.Empty, // Use ComboBox value
                    Status = cmbStatus.SelectedItem?.ToString() ?? "Created",
                SoldPrice = numSoldPrice.Value,
                Earnings = numEarnings.Value,
                SoldDate = string.Equals(cmbStatus.SelectedItem?.ToString(), "Sold", StringComparison.OrdinalIgnoreCase) ? dtSoldDate.Value.Date : (DateTime?)null,
                Brand = txtBrand.Text.Trim(),
                Colors = txtColors.Text.Trim(),
                ListingPrice = numListingPrice.Value,
                COG = numCOG.Value,
                SKU = txtSKU.Text.Trim(),
                ListingPlatform = string.Join(", ", chkPlatform.CheckedItems.Cast<string>()),
                CreatedAt = _isNew ? DateTime.Now : _item.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}