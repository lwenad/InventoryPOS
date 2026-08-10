using System;
using System.ComponentModel;
using System.Drawing;
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
        private TextBox txtCondition = null!;
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

        public InventoryItem ResultItem { get; private set; } = null!;

        public InventoryEditForm(InventoryItem? item = null)
        {
            _item = item ?? new InventoryItem();
            _isNew = item == null;

            // 1. Initialize strictly standard designer properties
            InitializeComponent();

            // 2. Set dynamic properties and custom UI
            this.Text = _isNew ? "Add Inventory Item" : "Edit Inventory Item";
            SetupCustomUI();

            // 3. Populate data
            LoadItemData();
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
            this.ClientSize = new Size(580, 660); // Slightly increased height to fit the taller list box
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Inventory Item";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupCustomUI()
        {
            this.SuspendLayout();

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            this.Controls.Add(mainPanel);

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

            // Status
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
            var isSold = string.Equals(cmbStatus.SelectedItem?.ToString(), "Sold", StringComparison.OrdinalIgnoreCase);
            // Ensure sold price and sold date controls reflect the current selected status
            numSoldPrice.Enabled = isSold;
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
            // If status is Sold, enable sold date and set to today if not present
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

            // If status is Sold, ensure a sold price is provided (> 0)
            var selectedStatus = cmbStatus.SelectedItem?.ToString() ?? string.Empty;
            if (string.Equals(selectedStatus, "Sold", StringComparison.OrdinalIgnoreCase) && numSoldPrice.Value <= 0m)
            {
                MessageBox.Show("Sold items must have a Sold Price greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numSoldPrice.Focus();
                return;
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