using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using InventoryPOS.Services;

namespace InventoryPOS.Forms
{
    public class ErrorDialogForm : Form
    {
        private readonly LoggerService _logger;
        private readonly Exception _exception;
        private readonly string _title;
        private RichTextBox txtDetails = null!;
        private Button btnCopy = null!;
        private Button btnClose = null!;

        public ErrorDialogForm(Exception ex, string title = "Error")
        {
            _logger = LoggerService.Instance;
            _exception = ex ?? new Exception("Unknown error");
            _title = title ?? "Error";

            // Log the exception details for troubleshooting
            _logger.LogCritical($"Error dialog shown: {_title}", _exception);

            InitializeComponent();
            LoadException(_exception);
        }

        private void InitializeComponent()
        {
            this.Text = _title;
            this.ClientSize = new Size(700, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            txtDetails = new RichTextBox
            {
                Location = new Point(12, 12),
                Size = new Size(676, 340),
                ReadOnly = true,
                BackColor = SystemColors.Window,
                Font = new Font("Consolas", 9F)
            };

            btnCopy = new Button
            {
                Text = "Copy",
                Location = new Point(12, 362),
                Size = new Size(90, 30)
            };
            btnCopy.Click += BtnCopy_Click;

            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(598, 362),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK
            };

            this.Controls.Add(txtDetails);
            this.Controls.Add(btnCopy);
            this.Controls.Add(btnClose);
        }

        private void LoadException(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("An unhandled exception occurred:");
            sb.AppendLine();
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine();
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(ex.StackTrace ?? "(no stack trace)");

            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("Inner Exception:");
                sb.AppendLine(ex.InnerException.ToString());
            }

            // Show some additional environment info
            sb.AppendLine();
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"CLR: {Environment.Version}");

            txtDetails.Text = sb.ToString();
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txtDetails.Text);
                MessageBox.Show(this, "Exception details copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                try
                {
                    MessageBox.Show(this, "Failed to copy to clipboard.", "Copy Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch { }
            }
        }
    }
}
