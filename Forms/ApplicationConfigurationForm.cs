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

            // Form settings
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(550, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Application Configuration";

            // Picture Folder Path Label
            var lblPictureFolder = new Label
            {
                Text = "Picture Folder:",
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblPictureFolder);

            // Picture Folder Path TextBox
            txtPictureFolderPath = new TextBox
            {
                Location = new Point(130, 28),
                Size = new Size(300, 25),
                ReadOnly = true
            };
            this.Controls.Add(txtPictureFolderPath);

            // Browse Button
            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(440, 27),
                Size = new Size(90, 27)
            };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            // Help Label
            var lblHelp = new Label
            {
                Text = "Pictures will be organized in SKU subfolders under this location.",
                Location = new Point(130, 60),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            this.Controls.Add(lblHelp);

            // Save Button
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(340, 120),
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
                Location = new Point(440, 120),
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

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var updatedState = new UiState
            {
                SortColumn = _uiState.SortColumn,
                SortOrder = _uiState.SortOrder,
                FilterColumn = _uiState.FilterColumn,
                FilterValue = _uiState.FilterValue,
                LastFilePath = _uiState.LastFilePath,
                PictureFolderPath = txtPictureFolderPath.Text.Trim(),
                HiddenColumns = _uiState.HiddenColumns
            };

            _onSave(updatedState);
            _logger.LogInfo($"Configuration saved. PictureFolderPath: '{updatedState.PictureFolderPath}'");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}