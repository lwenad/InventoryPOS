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
        private TextBox txtSize = null!;
        private TextBox txtCondition = null!;
        private TextBox txtBrand = null!;
        private TextBox txtColors = null!;
        private NumericUpDown numListingPrice = null!;
        private NumericUpDown numCOG = null!;
        private TextBox txtSKU = null!;
        private CheckedListBox chkPlatform = null!; // Changed to CheckedListBox
        private Button btnSave = null!;
        private Button btnCancel = null!;

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

            // Size
            AddLabel("Size", 20, y, labelWidth);
            txtSize = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Condition
            AddLabel("Condition", 20, y, labelWidth);
            txtCondition = AddTextBox(130, y, controlWidth);
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

            // SKU
            AddLabel("SKU", 20, y, labelWidth);
            txtSKU = AddTextBox(130, y, controlWidth);
            y += spacing;

            // Platform (Updated UI)
            AddLabel("Platform", 20, y, labelWidth);
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
            txtSize.Text = _item.Size;
            txtCondition.Text = _item.Condition;
            txtBrand.Text = _item.Brand;
            txtColors.Text = _item.Colors;
            numListingPrice.Value = Math.Min(Math.Max(_item.ListingPrice, numListingPrice.Minimum), numListingPrice.Maximum);
            numCOG.Value = Math.Min(Math.Max(_item.COG, numCOG.Minimum), numCOG.Maximum);
            txtSKU.Text = _item.SKU;

            // Platform multi-select load logic
            if (!string.IsNullOrWhiteSpace(_item.Platform))
            {
                var savedPlatforms = _item.Platform.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var platform in savedPlatforms)
                {
                    int index = chkPlatform.Items.IndexOf(platform.Trim());
                    if (index >= 0)
                    {
                        chkPlatform.SetItemChecked(index, true);
                    }
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

            ResultItem = new InventoryItem
            {
                Id = _item.Id,
                Title = txtTitle.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                SubCategory = txtSubCategory.Text.Trim(),
                Quantity = (int)numQuantity.Value,
                Size = txtSize.Text.Trim(),
                Condition = txtCondition.Text.Trim(),
                Brand = txtBrand.Text.Trim(),
                Colors = txtColors.Text.Trim(),
                ListingPrice = numListingPrice.Value,
                COG = numCOG.Value,
                SKU = txtSKU.Text.Trim(),

                // Platform multi-select save logic
                Platform = string.Join(", ", chkPlatform.CheckedItems.Cast<string>()),

                CreatedAt = _item.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}