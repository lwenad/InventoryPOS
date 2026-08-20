using System;
using System.IO;
using System.Windows.Forms;
using InventoryPOS.Models;
using InventoryPOS.Services;

namespace InventoryPOS.Forms
{
    public partial class ApplicationConfigurationForm : Form
    {
        private readonly LoggerService _logger;
        private readonly UiState _uiState;
        private readonly Action<UiState> _onSave;

        private TextBox txtPictureFolderPath = null!;
        private Button btnBrowse = null!;
        private TextBox txtLogFolderPath = null!;
        private Button btnBrowseLog = null!;
        private CheckedListBox chkDefaultPlatforms = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public ApplicationConfigurationForm(UiState currentState, Action<UiState> onSave)
        {
            _logger = LoggerService.Instance;
            _logger.LogInfo("ApplicationConfigurationForm opened");
            _uiState = currentState;
            _onSave = onSave;
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings - Increased height to prevent overlapping
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(550, 275);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Application Configuration";

            // Picture Folder Path Label
            var lblPictureFolder = new Label
            {
                Text = "Picture Folder:",
                Location = new Point(20, 20),
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblPictureFolder);

            // Picture Folder Path TextBox
            txtPictureFolderPath = new TextBox
            {
                Location = new Point(135, 18),
                Size = new Size(295, 25),
                ReadOnly = true
            };
            this.Controls.Add(txtPictureFolderPath);

            // Browse Button (Picture)
            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(440, 17),
                Size = new Size(90, 27)
            };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            // Help Label for Pictures
            var lblHelp = new Label
            {
                Text = "Pictures will be organized in SKU subfolders under this location.",
                Location = new Point(135, 47),
                Size = new Size(395, 18),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.5F)
            };
            this.Controls.Add(lblHelp);

            // Log Folder Path Label
            var lblLogFolder = new Label
            {
                Text = "Log Folder:",
                Location = new Point(20, 75),
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblLogFolder);

            // Log Folder Path TextBox
            txtLogFolderPath = new TextBox
            {
                Location = new Point(135, 73),
                Size = new Size(295, 25),
                ReadOnly = true
            };
            this.Controls.Add(txtLogFolderPath);

            // Browse Log Button
            btnBrowseLog = new Button
            {
                Text = "Browse...",
                Location = new Point(440, 72),
                Size = new Size(90, 27)
            };
            btnBrowseLog.Click += BtnBrowseLog_Click;
            this.Controls.Add(btnBrowseLog);

            // Default Listing Platforms Label
            var lblDefaultPlatform = new Label
            {
                Text = "Default Listing Platforms:",
                Location = new Point(20, 110),
                Size = new Size(160, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblDefaultPlatform);

            // Default Listing Platforms CheckedListBox
            chkDefaultPlatforms = new CheckedListBox
            {
                Location = new Point(190, 108),
                Size = new Size(150, 75),
                Items = { "eBay", "Poshmark", "Depop" }
            };

            // Pre-select platforms that are in the saved default
            _logger.LogInfo($"Config form loading DefaultListingPlatforms: '{_uiState.DefaultListingPlatforms}'");
            if (!string.IsNullOrWhiteSpace(_uiState.DefaultListingPlatforms))
            {
                var savedPlatforms = _uiState.DefaultListingPlatforms
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var platform in savedPlatforms)
                {
                    int index = chkDefaultPlatforms.Items.IndexOf(platform.Trim());
                    if (index >= 0)
                    {
                        chkDefaultPlatforms.SetItemChecked(index, true);
                        _logger.LogInfo($"Config form pre-checked: {platform}");
                    }
                }
            }
            this.Controls.Add(chkDefaultPlatforms);

            // Max Images per SKU Label
            var lblMaxImages = new Label
            {
                Text = "Max Images/SKU:",
                Location = new Point(20, 193),
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblMaxImages);

            // Max Images per SKU NumericUpDown
            var nudMaxImages = new NumericUpDown
            {
                Location = new Point(135, 191),
                Size = new Size(150, 25),
                Minimum = 1,
                Maximum = 100,
                Value = _uiState.MaxImagesPerSku
            };
            nudMaxImages.ValueChanged += (s, e) =>
            {
                _uiState.MaxImagesPerSku = (int)nudMaxImages.Value;
            };
            this.Controls.Add(nudMaxImages);

            // Confirm Before Delete CheckBox
            var chkConfirmDelete = new CheckBox
            {
                Text = "Confirm before delete",
                Location = new Point(20, 226),
                Size = new Size(200, 24),
                Checked = _uiState.ConfirmBeforeDelete
            };
            chkConfirmDelete.CheckedChanged += (s, e) =>
            {
                _uiState.ConfirmBeforeDelete = chkConfirmDelete.Checked;
            };
            this.Controls.Add(chkConfirmDelete);

            // Save Button
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(340, 220),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Cancel Button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(440, 220),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadCurrentSettings()
        {
            txtPictureFolderPath.Text = _uiState.PictureFolderPath ?? string.Empty;
            txtLogFolderPath.Text = _uiState.LogFolderPath ?? string.Empty;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the root folder for storing item pictures",
                ShowNewFolderButton = true,
                SelectedPath = !string.IsNullOrEmpty(_uiState.PictureFolderPath) && Directory.Exists(_uiState.PictureFolderPath)
                    ? _uiState.PictureFolderPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtPictureFolderPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseLog_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder for storing log files",
                ShowNewFolderButton = true,
                SelectedPath = !string.IsNullOrEmpty(_uiState.LogFolderPath) && Directory.Exists(_uiState.LogFolderPath)
                    ? _uiState.LogFolderPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtLogFolderPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Gather the currently checked platforms from the CheckedListBox
            var selected = new List<string>();
            for (int i = 0; i < chkDefaultPlatforms.Items.Count; i++)
            {
                if (chkDefaultPlatforms.GetItemChecked(i))
                {
                    selected.Add(chkDefaultPlatforms.Items[i].ToString()!);
                }
            }
            var platformsToSave = string.Join(", ", selected);
            _logger.LogInfo($"Config form saving DefaultListingPlatforms: '{platformsToSave}'");

            var updatedState = new UiState
            {
                SortColumn = _uiState.SortColumn,
                SortOrder = _uiState.SortOrder,
                FilterColumn = _uiState.FilterColumn,
                FilterValue = _uiState.FilterValue,
                LastFilePath = _uiState.LastFilePath,
                PictureFolderPath = txtPictureFolderPath.Text.Trim(),
                LogFolderPath = txtLogFolderPath.Text.Trim(),
                DefaultListingPlatforms = platformsToSave,
                MaxImagesPerSku = _uiState.MaxImagesPerSku,
                ConfirmBeforeDelete = _uiState.ConfirmBeforeDelete,
                HiddenColumns = _uiState.HiddenColumns
            };

            _onSave(updatedState);
            _logger.LogInfo($"Configuration saved. PictureFolderPath: '{updatedState.PictureFolderPath}', LogFolderPath: '{updatedState.LogFolderPath}', DefaultPlatforms: '{updatedState.DefaultListingPlatforms}', MaxImages: {updatedState.MaxImagesPerSku}");

            // Verify the log folder was created successfully
            if (!string.IsNullOrWhiteSpace(updatedState.LogFolderPath) && !Directory.Exists(updatedState.LogFolderPath))
            {
                MessageBox.Show(
                    $"Log folder could not be created: {updatedState.LogFolderPath}\nLogs will continue to use the default location.",
                    "Log Folder Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}