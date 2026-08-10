using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using InventoryPOS.Forms;
using InventoryPOS.Models;
using InventoryPOS.Services;

namespace InventoryPOS
{
    public partial class MainForm : Form
    {
        private readonly InventoryRepository _repository;
        private DataGridView dgvInventory = null!;
        private BindingSource _bindingSource = null!;
        private BindingList<InventoryItem> _bindingList = null!;
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem menuFile = null!;
        private ToolStripMenuItem menuFileLoad = null!;
        private ToolStripMenuItem menuFileSave = null!;
        private ToolStripMenuItem menuFileSaveAs = null!;
        private ToolStripSeparator menuFileSep1 = null!;
        private ToolStripMenuItem menuFileExit = null!;
        private ToolStrip toolStrip = null!;
        private ToolStripButton btnAdd = null!;
        private ToolStripButton btnEdit = null!;
        private ToolStripButton btnDelete = null!;
        private ToolStripButton btnRefresh = null!;
        private ToolStripTextBox txtSearch = null!;
        private ToolStripButton btnSearch = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblStatus = null!;
        private ToolStripStatusLabel lblCount = null!;
        private List<InventoryItem> _allItems = new();
        private ToolStripStatusLabel lblTotalCOG = null!;
        private Color _headerBackColor = Color.FromArgb(240, 240, 240);
        private Color _headerForeColor = Color.Black;

        public MainForm()
        {
            _repository = new InventoryRepository();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            //
            // MainForm
            //
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 800);
            this.Name = "MainForm";
            this.Text = "InventoryPOS - Clothing Inventory for Resale";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Load += MainForm_Load;

            CreateMenuStrip();
            CreateToolStrip();
            CreateDataGridView();
            CreateStatusStrip();
            menuStrip.SendToBack();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 9F)
            };

            menuFile = new ToolStripMenuItem("&File");

            menuFileLoad = new ToolStripMenuItem("&Load Inventory...", null, MenuFileLoad_Click)
            {
                ShortcutKeys = Keys.Control | Keys.O,
                ToolTipText = "Load inventory from a JSON file"
            };

            menuFileSave = new ToolStripMenuItem("&Save", null, MenuFileSave_Click)
            {
                ShortcutKeys = Keys.Control | Keys.S,
                ToolTipText = "Save inventory to current file"
            };

            menuFileSaveAs = new ToolStripMenuItem("Save &As...", null, MenuFileSaveAs_Click)
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.S,
                ToolTipText = "Save inventory to a new JSON file"
            };

            menuFileSep1 = new ToolStripSeparator();

            menuFileExit = new ToolStripMenuItem("E&xit", null, MenuFileExit_Click)
            {
                ShortcutKeys = Keys.Alt | Keys.F4
            };

            menuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuFileLoad,
                menuFileSave,
                menuFileSaveAs,
                menuFileSep1,
                menuFileExit
            });

            menuStrip.Items.Add(menuFile);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void MenuFileLoad_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Load Inventory",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "json",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _ = LoadFromFileAsync(dialog.FileName);
            }
        }

        private async Task LoadFromFileAsync(string filePath)
        {
            try
            {
                lblStatus.Text = "Loading...";
                _repository.SetFilePath(filePath);
                _allItems = await _repository.GetAllAsync();
                BindGrid(_allItems);
                UpdateCount();
                lblStatus.Text = $"Loaded {_allItems.Count} items from {Path.GetFileName(filePath)}";
                this.Text = $"InventoryPOS - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading file";
                MessageBox.Show($"Failed to load inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileSave_Click(object? sender, EventArgs e)
        {
            _ = SaveToCurrentFileAsync();
        }

        private async Task SaveToCurrentFileAsync()
        {
            try
            {
                lblStatus.Text = "Saving...";
                await _repository.SaveAllAsync(_allItems);
                lblStatus.Text = $"Saved to {Path.GetFileName(_repository.CurrentFilePath)}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error saving file";
                MessageBox.Show($"Failed to save inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileSaveAs_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Inventory As",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "inventory.json"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _ = SaveToFileAsync(dialog.FileName);
            }
        }

        private async Task SaveToFileAsync(string filePath)
        {
            try
            {
                lblStatus.Text = "Saving...";
                await _repository.SaveAllAsync(_allItems, filePath);
                _repository.SetFilePath(filePath);
                lblStatus.Text = $"Saved to {Path.GetFileName(filePath)}";
                this.Text = $"InventoryPOS - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error saving file";
                MessageBox.Show($"Failed to save inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileExit_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Padding = new Padding(5, 5, 5, 5),
                Font = new Font("Segoe UI", 9F),
                ImageScalingSize = new Size(20, 20)
            };

            btnAdd = new ToolStripButton("Add Item")
            {
                ToolTipText = "Add new inventory item (Ctrl+N)",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new ToolStripButton("Edit")
            {
                ToolTipText = "Edit selected item (Enter)",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Enabled = false
            };
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new ToolStripButton("Delete")
            {
                ToolTipText = "Delete selected item (Del)",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Enabled = false,
                ForeColor = Color.Red
            };
            btnDelete.Click += BtnDelete_Click;

            var separator1 = new ToolStripSeparator();

            btnRefresh = new ToolStripButton("Refresh")
            {
                ToolTipText = "Refresh data (F5)",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            btnRefresh.Click += BtnRefresh_Click;

            var separator2 = new ToolStripSeparator();

            var lblSearch = new ToolStripLabel("Search:");

            txtSearch = new ToolStripTextBox
            {
                Name = "txtSearch",
                Size = new Size(200, 25),
                ToolTipText = "Search by title, SKU, brand, category..."
            };
            txtSearch.KeyDown += TxtSearch_KeyDown;

            btnSearch = new ToolStripButton("Search")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            btnSearch.Click += BtnSearch_Click;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnAdd, btnEdit, btnDelete,
                separator1,
                btnRefresh,
                separator2,
                lblSearch, txtSearch, btnSearch
            });

            this.Controls.Add(toolStrip);
        }

        private void CreateDataGridView()
        {
            dgvInventory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersHeight = 35,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 30 },
                GridColor = Color.FromArgb(224, 224, 224)
            };

            dgvInventory.AutoGenerateColumns = false;

            // Use a BindingSource to back the grid. BindingSource works with the CurrencyManager
            // and avoids IndexOutOfRange exceptions when the underlying list changes.
            _bindingSource = new BindingSource();
            dgvInventory.DataSource = _bindingSource;

            dgvInventory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 5, 0),
                // Keep header appearance unchanged when rows/cells are selected
                SelectionBackColor = Color.FromArgb(240, 240, 240),
                SelectionForeColor = Color.Black
            };

            dgvInventory.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(5, 0, 5, 0),
                SelectionBackColor = Color.FromArgb(0, 122, 204),
                SelectionForeColor = Color.White
            };

            dgvInventory.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 248, 248)
            };

            // Ensure row selection uses the subtle gray instead of system blue
            dgvInventory.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 224, 224);
            dgvInventory.RowsDefaultCellStyle.SelectionForeColor = Color.Black;

            // Define columns
            dgvInventory.Columns.AddRange(new DataGridViewColumn[]
            {
                CreateColumn("Status", "Status", 100, false),
                CreateColumn("SKU", "SKU", 100, false),
                CreateColumn("Brand", "Brand", 120, false),
                CreateColumn("Category", "Category", 120, false),
                CreateColumn("SubCategory", "Sub Category", 120, false),
                CreateColumn("ListingPrice", "Listing Price", 100, false, DataGridViewContentAlignment.MiddleRight, "C2"),
                CreateColumn("COG", "COG", 100, false, DataGridViewContentAlignment.MiddleRight, "C2"),
                CreateColumn("SoldPrice", "Sold Price", 100, false, DataGridViewContentAlignment.MiddleRight, "C2"),
                CreateColumn("Condition", "Condition", 100, false),
                CreateColumn("Title", "Title", 200, false),
                CreateColumn("Description", "Description", 250, false),
                CreateColumn("Quantity", "Qty", 60, false, DataGridViewContentAlignment.MiddleCenter),
                CreateColumn("Size", "Size", 80, false),
                CreateColumn("Colors", "Colors", 120, false),
                CreateColumn("ListingPlatform", "Listing Platform", 100, false)
            });

            dgvInventory.SelectionChanged += DgvInventory_SelectionChanged;
            dgvInventory.CellFormatting += DgvInventory_CellFormatting;
            dgvInventory.CellDoubleClick += DgvInventory_CellDoubleClick;
            dgvInventory.KeyDown += DgvInventory_KeyDown;
            dgvInventory.DataError += DgvInventory_DataError;
            dgvInventory.CurrentCellChanged += DgvInventory_CurrentCellChanged;

            // Forces the last column to stretch and fill the remaining white space
            dgvInventory.Columns[dgvInventory.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Controls.Add(dgvInventory);

            dgvInventory.BringToFront();
            // Initialize header styles to avoid header color change when cells are selected
            ResetHeaderStyles();
        }

        private void DgvInventory_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            // Suppress the default DataGridView error dialog and show a concise message.
            e.ThrowException = false;
            lblStatus.Text = "Grid data error";
            // Show the error briefly so user knows what happened and to aid debugging.
            MessageBox.Show(this, $"A data error occurred in the grid: {e.Exception?.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DgvInventory_CurrentCellChanged(object? sender, EventArgs e)
        {
            ResetHeaderStyles();
        }

        private void ResetHeaderStyles()
        {
            if (dgvInventory == null || dgvInventory.Columns == null) return;

            foreach (DataGridViewColumn col in dgvInventory.Columns)
            {
                // Apply the normal header colors and make sure selection colors match
                col.HeaderCell.Style.BackColor = _headerBackColor;
                col.HeaderCell.Style.ForeColor = _headerForeColor;
                col.HeaderCell.Style.SelectionBackColor = _headerBackColor;
                col.HeaderCell.Style.SelectionForeColor = _headerForeColor;
            }

            // Force a repaint so header changes take effect immediately
            dgvInventory.Refresh();
        }

        private DataGridViewColumn CreateColumn(string dataPropertyName, string headerText, int width, bool autoSize = false, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft, string? format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                Width = width,
                AutoSizeMode = autoSize ? DataGridViewAutoSizeColumnMode.AllCells : DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = alignment,
                    Format = format
                },
                MinimumWidth = 50
            };
            return column;
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip
            {
                SizingGrip = false
            };

            lblStatus = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblCount = new ToolStripStatusLabel("0 items")
            {
                TextAlign = ContentAlignment.MiddleRight,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            lblTotalCOG = new ToolStripStatusLabel("Total COG: $0.00")
            {
                TextAlign = ContentAlignment.MiddleRight,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblCount, lblTotalCOG });
            this.Controls.Add(statusStrip);
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                lblStatus.Text = "Loading...";
                _allItems = await _repository.GetAllAsync();
                BindGrid(_allItems);
                lblStatus.Text = "Ready";
                UpdateCount();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading data";
                MessageBox.Show($"Failed to load inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindGrid(List<InventoryItem> items)
        {
            // Keep master list reference
            _allItems = items;

            if (_bindingSource == null)
            {
                _bindingSource = new BindingSource();
            }

            // Create a BindingList so removals/updates notify the grid safely
            _bindingList = new BindingList<InventoryItem>(_allItems);
            _bindingSource.DataSource = _bindingList;
            dgvInventory.DataSource = _bindingSource;
            dgvInventory.ClearSelection();
        }

        private void UpdateCount()
        {
            lblCount.Text = $"{_allItems.Count} item{(_allItems.Count != 1 ? "s" : "")}";
            decimal totalCOG = _allItems.Sum(i => i.COG);
            lblTotalCOG.Text = $"Total COG: {totalCOG:C2}";
        }

        private void UpdateCount(List<InventoryItem> items)
        {
            lblCount.Text = $"{items.Count} item{(items.Count != 1 ? "s" : "")}";
            decimal totalCOG = items.Sum(i => i.COG);
            lblTotalCOG.Text = $"Total COG: {totalCOG:C2}";
        }

        private void DgvInventory_SelectionChanged(object? sender, EventArgs e)
        {
            var hasSelection = dgvInventory.SelectedRows.Count > 0;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
        }

        private void DgvInventory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.Value == null) return;

                var col = dgvInventory.Columns[e.ColumnIndex];
                // Color the Status text green when item is Sold
                if (string.Equals(col.DataPropertyName, "Status", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.Value.ToString() == "Sold")
                    {
                        e.CellStyle.ForeColor = Color.Green;
                        e.CellStyle.SelectionForeColor = Color.Green;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.Black;
                        e.CellStyle.SelectionForeColor = Color.Black;
                    }
                }
            }
            catch
            {
                // ignore formatting errors
            }
        }

        private void DgvInventory_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                EditSelectedItem();
            }
        }

        private void DgvInventory_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EditSelectedItem();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedItem();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                _ = LoadDataAsync();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                AddNewItem();
                e.Handled = true;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            AddNewItem();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            EditSelectedItem();
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            DeleteSelectedItem();
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            _ = LoadDataAsync();
        }

        private void TxtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PerformSearch();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                txtSearch.Clear();
                BindGrid(_allItems);
                e.Handled = true;
            }
        }

        private void BtnSearch_Click(object? sender, EventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            var searchText = txtSearch.Text?.Trim().ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                BindGrid(_allItems);
                return;
            }

            var filtered = _allItems.Where(item =>
                (item.Title?.ToLower().Contains(searchText) ?? false) ||
                (item.SKU?.ToLower().Contains(searchText) ?? false) ||
                (item.Brand?.ToLower().Contains(searchText) ?? false) ||
                (item.Category?.ToLower().Contains(searchText) ?? false) ||
                (item.SubCategory?.ToLower().Contains(searchText) ?? false) ||
                (item.Description?.ToLower().Contains(searchText) ?? false) ||
                (item.ListingPlatform?.ToLower().Contains(searchText) ?? false) ||
                (item.Condition?.ToLower().Contains(searchText) ?? false) ||
                (item.Size?.ToLower().Contains(searchText) ?? false) ||
                (item.Colors?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            BindGrid(filtered);
            UpdateCount(filtered);
            lblStatus.Text = $"Found {filtered.Count} of {_allItems.Count} items";
        }

        private void AddNewItem()
        {
            using var form = new InventoryEditForm();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _ = SaveNewItemAsync(form.ResultItem);
            }
        }

        private async Task SaveNewItemAsync(InventoryItem item)
        {
            try
            {
                lblStatus.Text = "Saving...";
                await _repository.AddAsync(item);
                _allItems.Insert(0, item);
                BindGrid(_allItems);
                UpdateCount();
                lblStatus.Text = "Item added successfully";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error saving item";
                MessageBox.Show($"Failed to add item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditSelectedItem()
        {
            if (dgvInventory.SelectedRows.Count == 0) return;

            var selectedItem = dgvInventory.SelectedRows[0].DataBoundItem as InventoryItem;
            if (selectedItem == null) return;

            using var form = new InventoryEditForm(selectedItem);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _ = SaveEditedItemAsync(form.ResultItem);
            }
        }

        private async Task SaveEditedItemAsync(InventoryItem item)
        {
            try
            {
                lblStatus.Text = "Saving...";
                await _repository.UpdateAsync(item);

                var index = _allItems.FindIndex(i => i.Id == item.Id);
                if (index >= 0)
                {
                    _allItems[index] = item;
                }

                BindGrid(_allItems);
                UpdateCount();
                lblStatus.Text = "Item updated successfully";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error saving item";
                MessageBox.Show($"Failed to update item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DeleteSelectedItem()
        {
            if (dgvInventory.SelectedRows.Count == 0) return;

            var selectedItem = dgvInventory.SelectedRows[0].DataBoundItem as InventoryItem;
            if (selectedItem == null) return;

            var result = MessageBox.Show(
                $"Delete '{selectedItem.Title}' (SKU: {selectedItem.SKU})?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                lblStatus.Text = "Deleting...";
                // Await repository deletion so we don't race with UI events.
                await _repository.DeleteAsync(selectedItem.Id);

                // Defer UI updates until after current mouse/event processing completes to avoid
                // CurrencyManager / DataGridView races (Index out of range).
                this.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        // Clear current cell to avoid DataGridView trying to access the removed row
                        if (dgvInventory.CurrentCell != null)
                        {
                            dgvInventory.CurrentCell = null;
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    // Remove from binding list so grid updates safely.
                    if (_bindingList != null && _bindingList.Contains(selectedItem))
                    {
                        _bindingList.Remove(selectedItem);
                    }

                    // Keep master list in sync
                    _allItems.RemoveAll(i => i.Id == selectedItem.Id);

                    UpdateCount();
                    lblStatus.Text = "Item deleted";
                }));
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error deleting item";
                MessageBox.Show($"Failed to delete item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                AddNewItem();
                return true;
            }
            if (keyData == Keys.F5)
            {
                _ = LoadDataAsync();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}