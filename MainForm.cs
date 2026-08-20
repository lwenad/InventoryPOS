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
        private readonly LoggerService _logger;
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
        private ToolStripMenuItem menuConfiguration = null!;
        private ToolStripMenuItem menuProfitCalculator = null!;
        private ToolStripMenuItem menuView = null!;
        private ToolStripMenuItem menuColumns = null!;
        private HashSet<string> _hiddenColumns = new();
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
        private ToolStripStatusLabel lblTotalProfit = null!;
        private ToolStripStatusLabel lblTotalEarnings = null!;
        private Color _headerBackColor = Color.FromArgb(240, 240, 240);
        private Color _headerForeColor = Color.Black;
        private string? _currentFilterColumn;
        private string? _currentFilterValue;
        private readonly Dictionary<string, SortOrder> _sortStates = new();
        private List<InventoryItem> _displayList = new();
        private List<InventoryItem>? _lastDisplayBeforeSort;
        private List<InventoryItem>? _preFilterDisplay;
        private ToolStripStatusLabel lblFilterIndicator = null!;
    private ToolStripStatusLabel lblSoldCount = null!;
    private ToolStripStatusLabel lblCreatedCount = null!;
        private Dictionary<string, string> _originalHeaderTexts = new();
        private string? _pictureFolderPath;
        private readonly Dictionary<string, Image?> _photoCache = new(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            _repository = new InventoryRepository();
            _logger = LoggerService.Instance;
            _logger.LogInfo("MainForm initialized");
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
            this.Text = $"InventoryPOS v{VersionHelper.AppVersion} - Clothing Inventory for Resale";
            this.StartPosition = FormStartPosition.CenterScreen;
            // Start maximized to use full screen on startup
            this.WindowState = FormWindowState.Maximized;
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

            menuConfiguration = new ToolStripMenuItem("&Configuration", null, MenuConfiguration_Click)
            {
                ShortcutKeys = Keys.Control | Keys.Oemcomma,
                ToolTipText = "Open application configuration"
            };
            menuStrip.Items.Add(menuConfiguration);

            menuProfitCalculator = new ToolStripMenuItem("&Profit Calculator", null, MenuProfitCalculator_Click)
            {
                ShortcutKeys = Keys.Control | Keys.P,
                ToolTipText = "Open profit calculator for eBay, Poshmark, and Depop"
            };
            menuStrip.Items.Add(menuProfitCalculator);

            menuView = new ToolStripMenuItem("&View");
            menuColumns = new ToolStripMenuItem("C&olumns")
            {
                DropDownDirection = ToolStripDropDownDirection.Right
            };
            menuView.DropDownItems.Add(menuColumns);
            menuStrip.Items.Add(menuView);

            // Help menu with About
            var menuHelp = new ToolStripMenuItem("&Help");
            var menuAbout = new ToolStripMenuItem("&About", null, MenuAbout_Click)
            {
                ShortcutKeys = Keys.F1,
                ToolTipText = "About InventoryPOS"
            };
            menuHelp.DropDownItems.Add(menuAbout);
            menuStrip.Items.Add(menuHelp);

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
                _logger.LogInfo($"Load file requested: {filePath}");
                lblStatus.Text = "Loading...";
                _repository.SetFilePath(filePath);
                _allItems = await _repository.GetAllAsync();
                BindGrid(_allItems);
                UpdateCount();
                lblStatus.Text = $"Loaded {_allItems.Count} items from {Path.GetFileName(filePath)}";
                this.Text = $"InventoryPOS - {Path.GetFileName(filePath)}";
                // persist last opened file path to UI state
                var ui = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(ui);
                _logger.LogInfo($"Successfully loaded {_allItems.Count} items from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load file: {filePath}", ex);
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
                _logger.LogInfo($"Save to current file requested: {_repository.CurrentFilePath}");
                lblStatus.Text = "Saving...";
                await _repository.SaveAllAsync(_allItems);
                lblStatus.Text = $"Saved to {Path.GetFileName(_repository.CurrentFilePath)}";
                // persist last used file path in UI state
                var uiSaved = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(uiSaved);
                _logger.LogInfo($"Successfully saved {_allItems.Count} items to {_repository.CurrentFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save to current file: {_repository.CurrentFilePath}", ex);
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
                _logger.LogInfo($"Save As requested: {filePath}");
                lblStatus.Text = "Saving...";
                await _repository.SaveAllAsync(_allItems, filePath);
                _repository.SetFilePath(filePath);
                lblStatus.Text = $"Saved to {Path.GetFileName(filePath)}";
                this.Text = $"InventoryPOS - {Path.GetFileName(filePath)}";
                // persist last used file path in UI state after Save As
                var ui = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(ui);
                _logger.LogInfo($"Successfully saved {_allItems.Count} items to {filePath} (Save As)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save As to file: {filePath}", ex);
                lblStatus.Text = "Error saving file";
                MessageBox.Show($"Failed to save inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileExit_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuProfitCalculator_Click(object? sender, EventArgs e)
        {
            using var form = new ProfitCalculatorForm();
            form.ShowDialog(this);
        }

        private void MenuAbout_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                $"InventoryPOS\nVersion {Services.VersionHelper.AppVersion}\n\nClothing inventory management for resale.",
                "About InventoryPOS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void MenuConfiguration_Click(object? sender, EventArgs e)
        {
            // Load saved UI state first so preserved values (like DefaultListingPlatforms)
            // are available before we build the current state on top of them.
            var savedUiState = await _repository.LoadUiStateAsync();

            var currentUiState = BuildCurrentState();
            // Restore saved values that BuildCurrentState would otherwise reset
            if (savedUiState != null)
            {
                currentUiState.DefaultListingPlatforms = savedUiState.DefaultListingPlatforms;
                currentUiState.MaxImagesPerSku = savedUiState.MaxImagesPerSku;
                currentUiState.ConfirmBeforeDelete = savedUiState.ConfirmBeforeDelete;
                currentUiState.LogFolderPath = savedUiState.LogFolderPath;
                currentUiState.PictureFolderPath = savedUiState.PictureFolderPath;
            }

            using var form = new ApplicationConfigurationForm(currentUiState, OnConfigurationSaved);
            form.ShowDialog(this);
        }

        private async void OnConfigurationSaved(UiState updatedState)
        {
            await _repository.SaveUiStateAsync(updatedState);
            // Update the cached picture folder path and invalidate the thumbnail cache
            _pictureFolderPath = updatedState.PictureFolderPath;
            _photoCache.Clear();

            // Update the logger with any custom log folder path
            LoggerService.InitializeFromUiState(updatedState);

            lblStatus.Text = "Configuration saved";
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

            txtSearch = new ToolStripTextBox
            {
                Name = "txtSearch",
                Size = new Size(200, 25),
                ToolTipText = "Search by title, SKU, brand, category...",
                BackColor = Color.WhiteSmoke
            };
            txtSearch.KeyDown += TxtSearch_KeyDown;

            // Solid, visible border on the hosted TextBox inside ToolStripTextBox
            var host = txtSearch as ToolStripControlHost;
            if (host?.Control is TextBox tb)
            {
                tb.BorderStyle = BorderStyle.FixedSingle;
            }

            btnSearch = new ToolStripButton("Search")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSearch.Click += BtnSearch_Click;

            // Center the search between left-aligned buttons and right edge
            var springLeft = new ToolStripSeparator();
            var springRight = new ToolStripSeparator();
            springRight.Alignment = ToolStripItemAlignment.Right;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnAdd, btnEdit, btnDelete,
                separator1,
                btnRefresh,
                springLeft,
                btnSearch, txtSearch,
                springRight
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
                AllowUserToResizeRows = true,
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
                RowTemplate = { Height = 72 },
                GridColor = Color.FromArgb(224, 224, 224)
            };

            dgvInventory.AutoGenerateColumns = false;

            // Initialize BindingSource and BindingList once and bind now.  
            // We will mutate the BindingList contents later instead of reassigning DataSource,
            // which avoids CurrencyManager/DataGridView index races during startup and UI events.
            _bindingSource = new BindingSource();
            _bindingList = new BindingList<InventoryItem>();
            _bindingSource.DataSource = _bindingList;
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
                CreateImageColumn("Photo", "Photo", 70),
                CreateColumn("Status", "Status", 50, false),
                CreateColumn("SKU", "SKU", 100, true),
                CreateColumn("Brand", "Brand", 100, true),
                CreateColumn("Category", "Category", 100, false),
                CreateColumn("SubCategory", "Sub Category", 100, false),
                CreateColumn("ListingPrice", "Listing Price", 25, true, DataGridViewContentAlignment.MiddleCenter, "C2"),
                CreateColumn("COG", "COG", 25, true, DataGridViewContentAlignment.MiddleCenter, "C2"),
                CreateColumn("SoldPrice", "Sold Price", 25, true, DataGridViewContentAlignment.MiddleCenter, "C2"),
                CreateColumn("Earnings", "Earnings", 25, true, DataGridViewContentAlignment.MiddleCenter, "C2"),
                CreateColumn("Profit", "Profit", 25, true, DataGridViewContentAlignment.MiddleCenter, "C2"),
                CreateColumn("SoldDate", "Sold Date", 100, true, DataGridViewContentAlignment.MiddleCenter, "d"),
                CreateColumn("Condition", "Condition", 50, true),
                CreateColumn("Title", "Title", 100, false),
                CreateColumn("Description", "Description", 100, false),
                CreateColumn("Quantity", "Qty", 25, true, DataGridViewContentAlignment.MiddleCenter),
                CreateColumn("Size", "Size", 25, true),
                CreateColumn("Colors", "Colors", 50, true),
                CreateColumn("ListingPlatform", "Listing Platform", 80, false),
                // Show full date/time including seconds
                CreateColumn("CreatedAt", "Created", 150, false, DataGridViewContentAlignment.MiddleCenter, "G")
            });

            dgvInventory.SelectionChanged += DgvInventory_SelectionChanged;
            dgvInventory.CellFormatting += DgvInventory_CellFormatting;
            dgvInventory.CellDoubleClick += DgvInventory_CellDoubleClick;
            dgvInventory.KeyDown += DgvInventory_KeyDown;
            dgvInventory.DataError += DgvInventory_DataError;
            dgvInventory.ColumnHeaderMouseClick += DgvInventory_ColumnHeaderMouseClick;
            dgvInventory.CurrentCellChanged += DgvInventory_CurrentCellChanged;

            // Forces the last column to stretch and fill the remaining white space
            dgvInventory.Columns[dgvInventory.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Controls.Add(dgvInventory);

            dgvInventory.BringToFront();
            // Remember original header texts for filter indicator updates
            _originalHeaderTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewColumn c in dgvInventory.Columns)
            {
                if (!string.IsNullOrEmpty(c.DataPropertyName) && !_originalHeaderTexts.ContainsKey(c.DataPropertyName))
                    _originalHeaderTexts[c.DataPropertyName] = c.HeaderText;
            }

            // Initialize header styles to avoid header color change when cells are selected
            ResetHeaderStyles();

            // Build the View → Columns submenu from the columns defined above
            BuildColumnMenu();
        }

        private void DgvInventory_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            // Suppress the default DataGridView error dialog so we control what the user sees.
            e.ThrowException = false;

            var ex = e.Exception ?? new Exception("Unknown data error");
            _logger.LogWarning(
                $"Grid data error (row={e.RowIndex}, col={e.ColumnIndex})", ex);

            // IndexOutOfRangeException from DataGridViewDataConnection.GetError(rowIndex)
            // indicates a transient row-index sync issue (e.g. the grid tried to read a
            // row that no longer exists after the data source was refreshed). These are
            // self-healing — the grid recovers on the next paint — so we log them as a
            // warning and update the status bar without alarming the user with a dialog.
            if (ex is IndexOutOfRangeException)
            {
                lblStatus.Text = "Grid refreshed";
                return;
            }

            lblStatus.Text = "Grid data error";
            // Show the error briefly so user knows what happened and to aid debugging.
            MessageBox.Show(this, $"A data error occurred in the grid: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DgvInventory_CurrentCellChanged(object? sender, EventArgs e)
        {
            ResetHeaderStyles();
        }

        private void DgvInventory_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            var col = dgvInventory.Columns[e.ColumnIndex];
            var propName = col.DataPropertyName;
            if (string.IsNullOrEmpty(propName)) return;

            if (e.Button == MouseButtons.Left)
            {
                // Toggle sort
                ToggleSort(propName);
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Show filter menu with distinct values
                var menu = new ContextMenuStrip();
                var clearItem = new ToolStripMenuItem("Clear Filter");
                clearItem.Click += (s, ev) =>
                {
                    _currentFilterColumn = null;
                    _currentFilterValue = null;
                    // Defer rebinding until after the header click processing finishes to avoid CurrencyManager races
                    this.BeginInvoke((Action)(() =>
                    {
                        // Restore pre-filter display if available, otherwise show master list
                        var restore = _preFilterDisplay != null && _preFilterDisplay.Count > 0 ? _preFilterDisplay : (_displayList.Count > 0 ? _displayList : _allItems);
                        _displayList = restore.ToList();
                        BindGrid(_displayList);
                        UpdateCount(_displayList);
                        lblStatus.Text = "Filter cleared";
                        UpdateFilterIndicator();

                        // persist UI state
                        var ui = BuildCurrentState();
                        _ = _repository.SaveUiStateAsync(ui);
                    }));
                };
                menu.Items.Add(clearItem);
                menu.Items.Add(new ToolStripSeparator());

                // SKU is filtered by individual letters (case-insensitive) rather
                // than by full SKU value, so users can narrow by the letters a SKU contains.
                var values = GetDistinctFilterValues(propName);

                foreach (var v in values.Take(100))
                {
                    var item = new ToolStripMenuItem(string.IsNullOrEmpty(v) ? "(blank)" : v);
                    item.Click += (s, ev) =>
                    {
                        _currentFilterColumn = propName;
                        _currentFilterValue = v;
                        ApplyColumnFilter(propName, v);
                    };
                    menu.Items.Add(item);
                }

                menu.Show(Cursor.Position);
            }
        }

        private void ToggleSort(string propName)
        {
            // Toggle between Ascending/Descending/None
            _sortStates.TryGetValue(propName, out var state);
            SortOrder next;
            if (state == SortOrder.None) next = SortOrder.Ascending;
            else if (state == SortOrder.Ascending) next = SortOrder.Descending;
            else next = SortOrder.None;

            // clear other sorts; only store if next is not None
            _sortStates.Clear();
            if (next != SortOrder.None)
                _sortStates[propName] = next;

            // Determine current source list (display list if available)
            var source = _displayList.Count > 0 ? _displayList.ToList() : _allItems.ToList();

            // If user is starting a new sort, save the current display ordering so we can restore it
            if (next != SortOrder.None)
            {
                _lastDisplayBeforeSort = source.ToList();
            }

            if (next == SortOrder.None)
            {
                // Restore previous displayed order if available, otherwise show master list
                if (_lastDisplayBeforeSort != null)
                {
                    BindGrid(_lastDisplayBeforeSort);
                    UpdateCount(_lastDisplayBeforeSort);
                }
                else
                {
                    BindGrid(_displayList.Count > 0 ? _displayList : _allItems);
                    UpdateCount(_displayList.Count > 0 ? _displayList : _allItems);
                }

                UpdateSortGlyphs();
                lblStatus.Text = "Sort cleared";
                return;
            }

            var asc = next == SortOrder.Ascending;
            var sorted = asc
                ? source.OrderBy(i => GetPropertyValue(i, propName), Comparer<object?>.Create(ComparePropertyValues)).ToList()
                : source.OrderByDescending(i => GetPropertyValue(i, propName), Comparer<object?>.Create(ComparePropertyValues)).ToList();

            // Defer rebinding to avoid header click processing races
            this.BeginInvoke((Action)(() =>
            {
                BindGrid(sorted);
                UpdateCount(sorted);
                UpdateSortGlyphs();
                lblStatus.Text = $"Sorted by {propName} ({(asc ? "asc" : "desc")})";
                _logger.LogInfo($"Sorted by {propName} ({(asc ? "asc" : "desc")})");

                // persist UI state
                var ui = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(ui);
            }));
        }

        private void ApplyColumnFilter(string propName, string? value)
        {
            if (string.IsNullOrEmpty(propName)) return;
            // Capture current display before applying a filter so Clear Filter can restore it
            if (_preFilterDisplay == null || _preFilterDisplay.Count == 0)
            {
                _preFilterDisplay = _displayList.Count > 0 ? _displayList.ToList() : _allItems.ToList();
            }
            var filtered = _allItems.Where(i => MatchesFilter(i, propName, value)).ToList();
            // Update current display list and show filter indicator
            _currentFilterColumn = propName;
            _currentFilterValue = value;
            // Defer the actual rebind to avoid interfering with header mouse handling
            this.BeginInvoke((Action)(() =>
            {
                _displayList = filtered;
                BindGrid(_displayList);
                UpdateCount(_displayList);
                lblStatus.Text = $"Filtered {propName} = '{value}' ({_displayList.Count} items)";
                UpdateFilterIndicator();

                // persist UI state
                var ui = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(ui);
            }));
        }

        private void UpdateFilterIndicator()
        {
            if (lblFilterIndicator == null) return;

            if (!string.IsNullOrEmpty(_currentFilterColumn))
            {
                var display = _currentFilterColumn == "Search"
                    ? $"Search: '{_currentFilterValue}'"
                    : $"Filter: {_currentFilterColumn} = '{_currentFilterValue}'";
                lblFilterIndicator.Text = display;
                // Add header text suffix to indicate which column is filtered
                try
                {
                    foreach (DataGridViewColumn col in dgvInventory.Columns)
                    {
                        if (string.Equals(col.DataPropertyName, _currentFilterColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            var orig = _originalHeaderTexts.ContainsKey(col.DataPropertyName) ? _originalHeaderTexts[col.DataPropertyName] : col.HeaderText;
                            col.HeaderText = orig + " (filtered)";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(col.DataPropertyName) && _originalHeaderTexts.ContainsKey(col.DataPropertyName))
                                col.HeaderText = _originalHeaderTexts[col.DataPropertyName];
                        }
                    }
                }
                catch
                {
                    // ignore header update failures
                }
            }
            else
            {
                lblFilterIndicator.Text = string.Empty;
                // restore original header texts
                try
                {
                    foreach (DataGridViewColumn col in dgvInventory.Columns)
                    {
                        if (!string.IsNullOrEmpty(col.DataPropertyName) && _originalHeaderTexts.ContainsKey(col.DataPropertyName))
                            col.HeaderText = _originalHeaderTexts[col.DataPropertyName];
                    }
                }
                catch { }
            }
        }

        private object? GetPropertyValue(InventoryItem item, string propName)
        {
            try
            {
                var prop = typeof(InventoryItem).GetProperty(propName);
                return prop?.GetValue(item);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compares two property values for sorting. Nulls sort first (top) in
        /// both ascending and descending order. DateTime values (CreatedAt is
        /// DateTime; SoldDate is DateTime?, which boxes to DateTime or null) are
        /// compared chronologically rather than as strings, which would sort
        /// dates like "2024-..." lexically and break chronological order.
        /// Everything else falls back to a case-insensitive string comparison.
        /// </summary>
        private static int ComparePropertyValues(object? a, object? b)
        {
            // Nulls sort first
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            // Proper chronological DateTime comparison.
            // (CreatedAt is DateTime; SoldDate is DateTime? whose non-null
            // values arrive boxed as DateTime and whose nulls arrive as null.)
            if (a is DateTime da && b is DateTime db)
                return da.CompareTo(db);

            // Fallback: case-insensitive string comparison for other types
            return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the distinct filter candidate values for a column, drawn from
        /// the full inventory (consistent with the existing filter behavior).
        /// Most columns return the distinct property values; the SKU column is a
        /// special case and returns the distinct alphabetic characters (case-
        /// insensitive) found across all SKU values, so users can narrow the
        /// list by the letters a SKU contains.
        /// </summary>
        private List<string> GetDistinctFilterValues(string propName)
        {
            var rawValues = _allItems
                .Select(i => GetPropertyValue(i, propName)?.ToString() ?? string.Empty)
                .ToList();

            if (string.Equals(propName, "SKU", StringComparison.OrdinalIgnoreCase))
            {
                // Normalize each letter to uppercase before dedup so that
                // case variants (e.g. 'a' and 'A') collapse into one option.
                // SKU filtering is case-insensitive (see MatchesFilter), so a
                // single uppercase representative is shown per letter.
                var seen = new HashSet<char>();
                var letters = new List<char>();
                foreach (var sv in rawValues)
                {
                    foreach (var c in sv)
                    {
                        if (!char.IsLetter(c)) continue;
                        var upper = char.ToUpperInvariant(c);
                        if (seen.Add(upper))
                            letters.Add(upper);
                    }
                }
                return letters.OrderBy(c => c).Select(c => c.ToString()).ToList();
            }

            // Default: distinct full values, case-insensitive
            return rawValues.Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(v => v)
                            .ToList();
        }

        /// <summary>
        /// Determines whether an item matches a filter value for the given column.
        /// Most columns use an exact (case-insensitive) match; the SKU column uses
        /// a case-insensitive "contains" match against the selected letter.
        /// </summary>
        private bool MatchesFilter(InventoryItem item, string propName, string? value)
        {
            var itemValue = GetPropertyValue(item, propName)?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(value)) return string.IsNullOrEmpty(itemValue);

            if (string.Equals(propName, "SKU", StringComparison.OrdinalIgnoreCase))
                return itemValue.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

            return string.Equals(itemValue, value, StringComparison.OrdinalIgnoreCase);
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
                // Clear any existing sort glyph (UpdateSortGlyphs will set the correct one)
                if (col.HeaderCell is DataGridViewColumnHeaderCell hdr)
                {
                    hdr.SortGlyphDirection = SortOrder.None;
                }
            }

            // Force a repaint so header changes take effect immediately
            dgvInventory.Refresh();
        }

        private void UpdateSortGlyphs()
        {
            if (dgvInventory == null || dgvInventory.Columns == null) return;

            // Clear all glyphs first
            foreach (DataGridViewColumn col in dgvInventory.Columns)
            {
                if (col.HeaderCell is DataGridViewColumnHeaderCell hdr)
                    hdr.SortGlyphDirection = SortOrder.None;
            }

            // If we have a single active sort state, apply its glyph
            if (_sortStates.Count == 1)
            {
                var kv = _sortStates.First();
                var propName = kv.Key;
                var sortOrder = kv.Value;
                if (sortOrder == SortOrder.None) return;

                // Find the column with matching DataPropertyName
                foreach (DataGridViewColumn col in dgvInventory.Columns)
                {
                    if (string.Equals(col.DataPropertyName, propName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (col.HeaderCell is DataGridViewColumnHeaderCell hdr)
                        {
                            hdr.SortGlyphDirection = sortOrder;
                        }
                        break;
                    }
                }
            }
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

        /// <summary>
        /// Creates a <see cref="DataGridViewImageColumn"/> for thumbnail display
        /// (used by the Photo column). The image is centered and zoomed to fit the cell.
        /// The column is unbound (no DataPropertyName) because the image value is set
        /// dynamically in <see cref="DgvInventory_CellFormatting"/>.
        /// </summary>
        private DataGridViewImageColumn CreateImageColumn(string name, string headerText, int width)
        {
            var column = new DataGridViewImageColumn
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                MinimumWidth = 70,
                // Ensure the cell template is an image cell (not text)
                CellTemplate = new DataGridViewImageCell()
            };
            return column;
        }

        #region Column Visibility

        /// <summary>
        /// Populates the View → Columns submenu with one checkable item per
        /// grid column. Each item's Checked state tracks column visibility.
        /// </summary>
        private void BuildColumnMenu()
        {
            menuColumns.DropDownItems.Clear();

            foreach (DataGridViewColumn col in dgvInventory.Columns)
            {
                if (string.IsNullOrEmpty(col.DataPropertyName)) continue;

                var item = new ToolStripMenuItem(col.HeaderText)
                {
                    CheckOnClick = true,
                    Checked = true,
                    Tag = col.DataPropertyName
                };
                item.CheckedChanged += ColumnMenuItem_CheckedChanged;
                menuColumns.DropDownItems.Add(item);
            }
        }

        /// <summary>
        /// Toggles column visibility when a Columns menu item is checked/unchecked.
        /// The last visible column cannot be hidden.
        /// </summary>
        private void ColumnMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item) return;
            var propName = item.Tag as string;
            if (string.IsNullOrEmpty(propName)) return;

            var col = dgvInventory.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => string.Equals(c.DataPropertyName, propName, StringComparison.OrdinalIgnoreCase));
            if (col == null) return;

            if (!item.Checked) // user is about to hide this column
            {
                var visibleCount = dgvInventory.Columns
                    .Cast<DataGridViewColumn>()
                    .Count(c => !string.IsNullOrEmpty(c.DataPropertyName) && (c.Visible || string.Equals(c.DataPropertyName, propName, StringComparison.OrdinalIgnoreCase)));

                if (visibleCount <= 1)
                {
                    // Don't allow hiding the last visible column
                    item.Checked = true;
                    return;
                }
            }

            col.Visible = item.Checked;
            UpdateHiddenColumns(propName, item.Checked);
            SaveColumnVisibility();
        }

        private void UpdateHiddenColumns(string propName, bool visible)
        {
            if (visible)
                _hiddenColumns.Remove(propName);
            else
                _hiddenColumns.Add(propName);
        }

        /// <summary>
        /// Restores column visibility from a saved list of hidden DataPropertyNames.
        /// Must be called after BuildColumnMenu so the menu checkboxes are synced too.
        /// </summary>
        private void ApplyColumnVisibility(List<string>? hiddenColumns)
        {
            _hiddenColumns = hiddenColumns != null
                ? new HashSet<string>(hiddenColumns, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            foreach (DataGridViewColumn col in dgvInventory.Columns)
            {
                if (string.IsNullOrEmpty(col.DataPropertyName)) continue;
                col.Visible = !_hiddenColumns.Contains(col.DataPropertyName);
            }

            // Sync menu checkboxes without firing the CheckedChanged handler
            foreach (ToolStripItem ti in menuColumns.DropDownItems)
            {
                if (ti is not ToolStripMenuItem mi || mi.Tag is not string pn) continue;
                mi.CheckedChanged -= ColumnMenuItem_CheckedChanged;
                var col = dgvInventory.Columns
                    .Cast<DataGridViewColumn>()
                    .FirstOrDefault(c => string.Equals(c.DataPropertyName, pn, StringComparison.OrdinalIgnoreCase));
                mi.Checked = col?.Visible ?? true;
                mi.CheckedChanged += ColumnMenuItem_CheckedChanged;
            }
        }

        #endregion

        /// <summary>
        /// Builds a UiState capturing the current sort, filter, file path,
        /// and column-visibility settings. Used by every state-persistence call
        /// so that no dimension of UI state is silently dropped when saving.
        /// </summary>
        private UiState BuildCurrentState()
        {
            return new UiState
            {
                SortColumn = _sortStates.Count == 1 ? _sortStates.Keys.First() : null,
                SortOrder = _sortStates.Count == 1 ? _sortStates.Values.First().ToString() : null,
                FilterColumn = _currentFilterColumn,
                FilterValue = _currentFilterValue,
                LastFilePath = _repository.CurrentFilePath,
                PictureFolderPath = _pictureFolderPath,
                LogFolderPath = _logger.GetCurrentLogFilePath(), // save the current log folder path
                DefaultListingPlatforms = // determined by last used profit calculator context
                    null, // TODO: could store last platform selection if desired
                MaxImagesPerSku = 20, // default, will be overridden by config
                ConfirmBeforeDelete = true, // default, will be overridden by config
                HiddenColumns = _hiddenColumns.Count > 0 ? _hiddenColumns.ToList() : null
            };
        }

        private void SaveColumnVisibility()
        {
            var ui = BuildCurrentState();
            _ = _repository.SaveUiStateAsync(ui);
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

            lblTotalProfit = new ToolStripStatusLabel("Total Profit: $0.00")
            {
                TextAlign = ContentAlignment.MiddleRight,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            lblTotalEarnings = new ToolStripStatusLabel("Total Earnings: $0.00")
            {
                TextAlign = ContentAlignment.MiddleRight,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            lblFilterIndicator = new ToolStripStatusLabel(string.Empty)
            {
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DarkBlue
            };

            lblCreatedCount = new ToolStripStatusLabel("Created: 0")
            {
                TextAlign = ContentAlignment.MiddleRight,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            lblSoldCount = new ToolStripStatusLabel("Sold: 0")
            {
                TextAlign = ContentAlignment.MiddleRight,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblFilterIndicator, lblCreatedCount, lblSoldCount, lblCount, lblTotalCOG, lblTotalEarnings, lblTotalProfit  });
            this.Controls.Add(statusStrip);
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            await LoadDataAsync();

            // Initialize logger from saved UI state so custom log folder path takes effect
            var ui = await _repository.LoadUiStateAsync();
            if (ui != null)
            {
                LoggerService.InitializeFromUiState(ui);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _logger.LogInfo("LoadDataAsync starting");
                lblStatus.Text = "Loading...";

                // Load UI state first so we can restore the last used data file path before loading data
                var ui = await _repository.LoadUiStateAsync();
                // Capture the picture folder path for the photo grid column
                if (ui?.PictureFolderPath != null)
                    _pictureFolderPath = ui.PictureFolderPath;

                if (ui != null && !string.IsNullOrEmpty(ui.LastFilePath) && System.IO.File.Exists(ui.LastFilePath))
                {
                    _repository.SetFilePath(ui.LastFilePath);
                }

                _allItems = await _repository.GetAllAsync();
                // Initialize display list and bind
                _displayList = _allItems.ToList();
                BindGrid(_displayList);
                // Restore persisted column visibility (BuildColumnMenu already ran in InitializeComponent)
                ApplyColumnVisibility(ui?.HiddenColumns);
                lblStatus.Text = "Ready";
                UpdateCount();
                _logger.LogInfo($"LoadDataAsync completed: {_allItems.Count} items loaded");

                // Load UI state (sort/filter) and apply asynchronously to avoid interfering with layout/event processing
                if (ui != null)
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            // apply filter first if present
                            if (!string.IsNullOrEmpty(ui.FilterColumn))
                            {
                                _currentFilterColumn = ui.FilterColumn;
                                _currentFilterValue = ui.FilterValue;
                                if (ui.FilterColumn == "Search")
                                {
                                    txtSearch.Text = ui.FilterValue ?? string.Empty;
                                    PerformSearch();
                                }
                                else
                                {
                                    ApplyColumnFilter(ui.FilterColumn, ui.FilterValue);
                                }
                            }

                            // apply saved sort
                            if (!string.IsNullOrEmpty(ui.SortColumn) && !string.IsNullOrEmpty(ui.SortOrder))
                            {
                                var ord = ui.SortOrder == "Ascending" ? SortOrder.Ascending : SortOrder.Descending;
                                _sortStates.Clear();
                                _sortStates[ui.SortColumn] = ord;

                                // Apply sort to current display list
                                var source = (_displayList != null && _displayList.Count > 0) ? _displayList.ToList() : _allItems.ToList();
                                var asc = ord == SortOrder.Ascending;
                                var sorted = asc
                                    ? source.OrderBy(i => GetPropertyValue(i, ui.SortColumn), Comparer<object?>.Create(ComparePropertyValues)).ToList()
                                    : source.OrderByDescending(i => GetPropertyValue(i, ui.SortColumn), Comparer<object?>.Create(ComparePropertyValues)).ToList();

                                BindGrid(sorted);
                                UpdateCount(sorted);
                                UpdateSortGlyphs();
                            }
                        }
                        catch (Exception ex)
                        {
                            // ignore UI apply failures
                            _logger.LogWarning("Failed to restore saved UI state (sort/filter)", ex);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load data on startup", ex);
                lblStatus.Text = "Error loading data";
                MessageBox.Show($"Failed to load inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindGrid(List<InventoryItem> items)
        {
            // Keep an in-memory display list reference
            _displayList = items ?? new List<InventoryItem>();

            if (_bindingList == null)
            {
                _bindingList = new BindingList<InventoryItem>();
            }

            // Update existing BindingList contents safely to avoid resetting DataSource during UI events
            try
            {
                // Prevent the grid from processing input/layout while we update the list
                dgvInventory.SuspendLayout();
                try { dgvInventory.Enabled = false; } catch { }

                _bindingList.RaiseListChangedEvents = false;
                _bindingList.Clear();
                foreach (var it in _displayList)
                {
                    _bindingList.Add(it);
                }
            }
            finally
            {
                _bindingList.RaiseListChangedEvents = true;
                try
                {
                    // Force=true is required here: because RaiseListChangedEvents was
                    // disabled during the Clear()/Add() cycle, no ListChanged(Reset)
                    // event was propagated to the grid, so its row count is still based
                    // on the previous list. ResetBindings(true) calls
                    // CurrencyManager.Refresh() which fires a full ListChanged(Reset),
                    // causing the DataGridView to rebuild all rows and sync its row
                    // count with the data source. Without this, a subsequent layout
                    // pass can call GetError(rowIndex) for a row that no longer exists,
                    // throwing IndexOutOfRangeException ("Index N does not have a value")
                    // — most visible when filtering produces a smaller result set.
                    _bindingSource?.ResetBindings(true);
                }
                catch
                {
                    // ResetBindings may throw if grid is in an odd state; ignore to avoid crashing the UI
                }

                try { dgvInventory.Enabled = true; } catch { }
                dgvInventory.ResumeLayout();
            }

            try { dgvInventory.CurrentCell = null; } catch { }
            dgvInventory.ClearSelection();
            UpdateSortGlyphs();

            // Force a repaint so image cells are rendered
            dgvInventory.Invalidate();
            dgvInventory.Refresh();
        }

        /// <summary>
        /// Restores the grid selection to the row whose <see cref="InventoryItem.Id"/>
        /// matches <paramref name="itemId"/>. Called after a rebind so the user's
        /// place in the grid is preserved after operations such as editing an item.
        /// </summary>
        private void RestoreGridSelection(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            for (int i = 0; i < dgvInventory.Rows.Count; i++)
            {
                if (dgvInventory.Rows[i].IsNewRow) continue;
                var rowItem = dgvInventory.Rows[i].DataBoundItem as InventoryItem;
                if (rowItem?.Id == itemId)
                {
                    dgvInventory.ClearSelection();
                    dgvInventory.Rows[i].Selected = true;
                    dgvInventory.CurrentCell = dgvInventory.Rows[i].Cells[0];
                    return;
                }
            }
        }

        /// <summary>
        /// Forces re-population of the Photo column cells after the thumbnail
        /// cache has been invalidated (e.g. after adding or removing pictures
        /// via the edit form). Directly sets cell values so the grid reflects
        /// the current on-disk state without requiring a full rebind.
        /// </summary>
        private void RefreshPhotoColumn()
        {
            // Find the Photo column index
            var photoColIdx = -1;
            for (int i = 0; i < dgvInventory.Columns.Count; i++)
            {
                if (string.Equals(dgvInventory.Columns[i].Name, "Photo", StringComparison.OrdinalIgnoreCase))
                {
                    photoColIdx = i;
                    break;
                }
            }
            if (photoColIdx < 0) return;

            // Set cell values directly from the cache (populated lazily by CellFormatting)
            for (int r = 0; r < dgvInventory.Rows.Count; r++)
            {
                if (dgvInventory.Rows[r].IsNewRow) continue;

                var cell = dgvInventory.Rows[r].Cells[photoColIdx];
                if (cell is DataGridViewImageCell imgCell)
                {
                    var item = dgvInventory.Rows[r].DataBoundItem as InventoryItem;
                    var sku = item?.SKU ?? string.Empty;

                    if (!_photoCache.TryGetValue(sku, out var cachedImage))
                    {
                        if (string.IsNullOrEmpty(_pictureFolderPath))
                        {
                            cachedImage = PictureService.GetPlaceholderImage();
                        }
                        else
                        {
                            var picPath = PictureService.GetFirstPicturePath(_pictureFolderPath, sku);
                            cachedImage = string.IsNullOrEmpty(picPath)
                                ? PictureService.GetPlaceholderImage()
                                : PictureService.LoadThumbnail(picPath);
                            if (cachedImage == null)
                                cachedImage = PictureService.GetPlaceholderImage();
                        }
                        _photoCache[sku] = cachedImage;
                    }

                    imgCell.Value = cachedImage;
                }
            }

            dgvInventory.Invalidate();
            dgvInventory.Refresh();
        }

        private void UpdateCount()
        {
            var items = (_displayList != null && _displayList.Count > 0) ? _displayList : _allItems;
            lblCount.Text = $"{items.Count} item{(items.Count != 1 ? "s" : "")}";
            int soldCount = items.Count(i => string.Equals(i.Status, "Sold", StringComparison.OrdinalIgnoreCase));
            int createdCount = items.Count(i => string.Equals(i.Status, "Created", StringComparison.OrdinalIgnoreCase));
            lblSoldCount.Text = $"Sold: {soldCount}";
            lblCreatedCount.Text = $"Created: {createdCount}";
            decimal totalCOG = items.Sum(i => i.COG);
            lblTotalCOG.Text = $"Total COG: {totalCOG:C2}";
            decimal totalProfit = items.Sum(i => i.Profit);
            lblTotalProfit.Text = $"Total Profit: {totalProfit:C2}";
            decimal totalEarnings = items.Sum(i => i.Earnings);
            lblTotalEarnings.Text = $"Total Earnings: {totalEarnings:C2}";
        }

        private void UpdateCount(List<InventoryItem> items)
        {
            lblCount.Text = $"{items.Count} item{(items.Count != 1 ? "s" : "")}";
            int soldCount = items.Count(i => string.Equals(i.Status, "Sold", StringComparison.OrdinalIgnoreCase));
            int createdCount = items.Count(i => string.Equals(i.Status, "Created", StringComparison.OrdinalIgnoreCase));
            lblSoldCount.Text = $"Sold: {soldCount}";
            lblCreatedCount.Text = $"Created: {createdCount}";
            decimal totalCOG = items.Sum(i => i.COG);
            lblTotalCOG.Text = $"Total COG: {totalCOG:C2}";
            decimal totalProfit = items.Sum(i => i.Profit);
            lblTotalProfit.Text = $"Total Profit: {totalProfit:C2}";
            decimal totalEarnings = items.Sum(i => i.Earnings);
            lblTotalEarnings.Text = $"Total Earnings: {totalEarnings:C2}";
        }

        /// <summary>
        /// Re-applies the current filter and/or sort to the display list after
        /// the underlying data has changed (e.g., after editing an item).
        /// This preserves any active column filter instead of resetting to the
        /// full list. If no filter/sort is active, the full list is shown.
        /// </summary>
        private void RestoreFilteredOrSortedDisplay()
        {
            // If a column filter is active, re-apply it to the (now updated) master list
            if (!string.IsNullOrEmpty(_currentFilterColumn) && _currentFilterColumn != "Search")
            {
                var filtered = _allItems
                    .Where(i => MatchesFilter(i, _currentFilterColumn, _currentFilterValue))
                    .ToList();
                _displayList = filtered;
                BindGrid(_displayList);
                UpdateCount(_displayList);
                UpdateFilterIndicator();
            }
            else if (_currentFilterColumn == "Search")
            {
                // Re-apply the text search filter instead of losing it
                PerformSearch();
            }
            else if (_sortStates.Count == 1)
            {
                // No filter, but a sort is active — re-sort the full list
                var kv = _sortStates.First();
                var asc = kv.Value == SortOrder.Ascending;
                var sorted = asc
                    ? _allItems.OrderBy(i => GetPropertyValue(i, kv.Key), Comparer<object?>.Create(ComparePropertyValues)).ToList()
                    : _allItems.OrderByDescending(i => GetPropertyValue(i, kv.Key), Comparer<object?>.Create(ComparePropertyValues)).ToList();
                _displayList = sorted;
                BindGrid(_displayList);
                UpdateCount(_displayList);
                UpdateSortGlyphs();
            }
            else
            {
                // No filter or sort — show the full list
                _displayList = _allItems.ToList();
                BindGrid(_displayList);
                UpdateCount(_displayList);
            }
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
                if (e.RowIndex < 0) return;

                var col = dgvInventory.Columns[e.ColumnIndex];

                // Photo column: resolve the thumbnail from the SKU folder
                // Use Name (not DataPropertyName) because the column is unbound
                if (string.Equals(col.Name, "Photo", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.RowIndex < _bindingList.Count)
                    {
                        var item = _bindingList[e.RowIndex];
                        var sku = item.SKU ?? string.Empty;

                        if (!_photoCache.TryGetValue(sku, out var cachedImage))
                        {
                            if (string.IsNullOrEmpty(_pictureFolderPath))
                            {
                                cachedImage = PictureService.GetPlaceholderImage();
                                _logger.LogInfo($"Photo column: PictureFolderPath is null/empty, showing placeholder for SKU '{sku}'");
                            }
                            else
                            {
                                var picPath = PictureService.GetFirstPicturePath(_pictureFolderPath, sku);
                                if (string.IsNullOrEmpty(picPath))
                                {
                                    cachedImage = PictureService.GetPlaceholderImage();
                                    _logger.LogInfo($"Photo column: No image found for SKU '{sku}' in '{_pictureFolderPath}', showing placeholder");
                                }
                                else
                                {
                                    cachedImage = PictureService.LoadThumbnail(picPath);
                                    if (cachedImage == null)
                                    {
                                        cachedImage = PictureService.GetPlaceholderImage();
                                        _logger.LogInfo($"Photo column: Failed to load thumbnail for SKU '{sku}' from '{picPath}', showing placeholder");
                                    }
                                    else
                                    {
                                        _logger.LogInfo($"Photo column: Loaded thumbnail for SKU '{sku}' from '{picPath}'");
                                    }
                                }
                            }
                            _photoCache[sku] = cachedImage;
                        }

                        e.Value = cachedImage;
                        e.FormattingApplied = true;
                    }
                    return;
                }

                if (e.Value == null) return;

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
            var searchText = txtSearch.Text?.Trim() ?? string.Empty;
            _logger.LogInfo($"Search initiated: '{searchText}'");
            if (string.IsNullOrEmpty(searchText))
            {
                // clear search filter
                _currentFilterColumn = null;
                _currentFilterValue = null;
                _displayList = _allItems.ToList();
                BindGrid(_displayList);
                UpdateFilterIndicator();

                // persist UI state
                var uiClear = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(uiClear);
                return;
            }

            var filtered = _allItems.Where(item =>
                (item.Title?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.SKU?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Brand?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Category?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.SubCategory?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.ListingPlatform?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Condition?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Size?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Colors?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            _logger.LogInfo($"Search '{searchText}' returned {filtered.Count} of {_allItems.Count} items");

            // mark as search filter
            _currentFilterColumn = "Search";
            _currentFilterValue = searchText;
            _displayList = filtered;
            // defer rebind to avoid interfering with key processing
            this.BeginInvoke((Action)(() =>
            {
                BindGrid(_displayList);
                UpdateCount(_displayList);
                lblStatus.Text = $"Found {_displayList.Count} of {_allItems.Count} items";
                UpdateFilterIndicator();

                // persist UI state
                var ui = BuildCurrentState();
                _ = _repository.SaveUiStateAsync(ui);
            }));
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
                _logger.LogInfo($"Adding new item. SKU: {item.SKU}, Title: {item.Title}");
                lblStatus.Text = "Saving...";
                await _repository.AddAsync(item);
                _allItems.Insert(0, item);

                // Invalidate the photo cache so new/edited items reflect their pictures
                _photoCache.Clear();

                // Re-bind preserving any active filter or sort so the column
                // filter is not cleared after adding a new item.
                RestoreFilteredOrSortedDisplay();
                lblStatus.Text = "Item added successfully";
                _logger.LogInfo($"Item added successfully. SKU: {item.SKU}, Id: {item.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add new item. SKU: {item.SKU}, Title: {item.Title}", ex);
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
            var result = form.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                // Clear the photo cache — pictures may have been added or removed
                // via the Picture Management tab in the edit form.
                _photoCache.Clear();
                _ = SaveEditedItemAsync(form.ResultItem);
            }
            else if (form.PicturesChanged)
            {
                // Pictures were added/removed while editing but the user did not
                // save other item changes. Invalidate the photo cache so the
                // main grid reflects on-disk picture changes without requiring
                // an application restart.
                _photoCache.Clear();
                RestoreFilteredOrSortedDisplay();
                // Restore selection to the edited item and refresh the photo column
                // (deferred to avoid races with the grid rebuilding rows).
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    RestoreGridSelection(selectedItem.Id);
                    RefreshPhotoColumn();
                }));
            }
        }

        private async Task SaveEditedItemAsync(InventoryItem item)
        {
            try
            {
                _logger.LogInfo($"Updating item. SKU: {item.SKU}, Title: {item.Title}, Id: {item.Id}");
                lblStatus.Text = "Saving...";
                await _repository.UpdateAsync(item);

                var index = _allItems.FindIndex(i => i.Id == item.Id);
                if (index >= 0)
                {
                    _allItems[index] = item;
                }

                // Invalidate the photo cache so edited items reflect their updated pictures
                _photoCache.Clear();

                // Re-bind preserving any active filter or sort so the column
                // filter is not cleared after editing an item.
                RestoreFilteredOrSortedDisplay();

                // Ensure sort is applied and visible after editing.
                // RestoreFilteredOrSortedDisplay() may re-sort if _sortStates has one entry,
                // but we explicitly trigger the sort update so the UI reflects the correct
                // sort order and glyph immediately after the edit.
                UpdateSortGlyphs();

                // Restore selection to the edited item and refresh the photo column
                // (deferred in a single BeginInvoke to avoid races with the grid
                // rebuilding rows caused by RestoreFilteredOrSortedDisplay calling
                // BindGrid which clears the selection). Refresh first so the
                // photo cells are ready, then restore selection.
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    RefreshPhotoColumn();
                    RestoreGridSelection(item.Id);
                }));

                lblStatus.Text = "Item updated successfully";
                _logger.LogInfo($"Item updated successfully. SKU: {item.SKU}, Id: {item.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update item. SKU: {item.SKU}, Title: {item.Title}", ex);
                lblStatus.Text = "Error saving item";
                MessageBox.Show($"Failed to update item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DeleteSelectedItem()
        {
            if (dgvInventory.SelectedRows.Count == 0) return;

            var selectedItem = dgvInventory.SelectedRows[0].DataBoundItem as InventoryItem;
            if (selectedItem == null) return;

            // Check if confirmation is required per configuration
            bool confirmBeforeDelete = await _repository.LoadUiStateAsync() is { } uiState
                && uiState.ConfirmBeforeDelete;
            if (confirmBeforeDelete)
            {
                var result = MessageBox.Show(
                    $"Delete '{selectedItem.Title}' (SKU: {selectedItem.SKU})?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes) return;
            }

            try
            {
                _logger.LogInfo($"Delete item requested. SKU: {selectedItem.SKU}, Title: {selectedItem.Title}, Id: {selectedItem.Id}");
                lblStatus.Text = "Deleting...";

                // Capture the selected row index so we can remove by index later on the UI thread.
                var selectedRowIndex = -1;
                try
                {
                    if (dgvInventory.SelectedRows.Count > 0)
                        selectedRowIndex = dgvInventory.SelectedRows[0].Index;
                }
                catch { }

                var idToRemove = selectedItem.Id;

                // Await repository deletion so we don't race with UI events.
                await _repository.DeleteAsync(idToRemove);

                // Defer UI updates until after current mouse/event processing completes to avoid
                // CurrencyManager / DataGridView races (Index out of range).
                this.BeginInvoke((Action)(() =>
                {
                    // Temporarily disable UI updates on the grid while we remove the row
                    try { dgvInventory.SuspendLayout(); } catch { }
                    try { dgvInventory.Enabled = false; } catch { }

                    try
                    {
                        // Clear current cell to avoid DataGridView trying to access the removed row
                        try
                        {
                            if (dgvInventory.CurrentCell != null)
                            {
                                dgvInventory.CurrentCell = null;
                            }
                        }
                        catch { /* ignore */ }

                        // Try to remove by the previously-captured index first (fast and consistent with current view)
                        var removed = false;
                        if (selectedRowIndex >= 0 && _bindingList != null && selectedRowIndex < _bindingList.Count)
                        {
                            try
                            {
                                _bindingList.RemoveAt(selectedRowIndex);
                                removed = true;
                            }
                            catch { removed = false; }
                        }

                        // Fallback: remove by Id if index removal failed
                        if (!removed && _bindingList != null)
                        {
                            var match = _bindingList.FirstOrDefault(x => x.Id == idToRemove);
                            if (match != null)
                            {
                                _bindingList.Remove(match);
                                removed = true;
                            }
                        }

                        // Keep master list and display lists in sync
                        _allItems.RemoveAll(i => i.Id == idToRemove);
                        if (_displayList != null && _displayList.Any())
                            _displayList.RemoveAll(i => i.Id == idToRemove);
                        if (_preFilterDisplay != null && _preFilterDisplay.Any())
                            _preFilterDisplay.RemoveAll(i => i.Id == idToRemove);

                        // Remove the deleted item's cached photo (if any)
                        if (!string.IsNullOrEmpty(selectedItem.SKU))
                            _photoCache.Remove(selectedItem.SKU);

                        // If nothing removed above, rebind as a final fallback
                        if (!removed)
                        {
                            try { BindGrid(_allItems); } catch { }
                        }

                        UpdateCount();
                        lblStatus.Text = "Item deleted";
                        _logger.LogInfo($"Item deleted successfully. Id: {idToRemove}");
                    }
                    catch (Exception ex)
                    {
                        // If anything goes wrong updating the UI, show a non-fatal message and refresh the whole grid as a fallback
                        _logger.LogError($"Failed to update UI after delete. Id: {idToRemove}", ex);
                        try
                        {
                            BindGrid(_allItems);
                            UpdateCount();
                        }
                        catch { }
                        lblStatus.Text = "Error updating UI after delete";
                        MessageBox.Show($"Deleted from storage but failed to update grid: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    try { dgvInventory.Enabled = true; } catch { }
                    try { dgvInventory.ResumeLayout(); } catch { }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete item. Id: {selectedItem.Id}", ex);
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