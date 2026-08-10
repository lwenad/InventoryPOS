using System;
using System.Windows.Forms;

namespace InventoryPOS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Catch unhandled exceptions on UI thread
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                try { ShowException(e.Exception, "Unhandled UI Exception"); }
                catch { }
            };

            // Catch non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception ?? new Exception(e.ToString());
                    ShowException(ex, "Unhandled Thread Exception");
                }
                catch { }
            };

            // Task scheduler exceptions
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                try
                {
                    ShowException(e.Exception, "Unobserved Task Exception");
                    e.SetObserved();
                }
                catch { }
            };

            Application.Run(new MainForm());
        }

        internal static void ShowException(Exception ex, string title)
        {
            try
            {
                // If we have an open UI thread, invoke the dialog there
                if (Application.OpenForms != null && Application.OpenForms.Count > 0)
                {
                    var owner = Application.OpenForms[0];
                    try
                    {
                        if (owner.InvokeRequired)
                        {
                            owner.BeginInvoke((Action)(() => new Forms.ErrorDialogForm(ex, title).ShowDialog(owner)));
                        }
                        else
                        {
                            new Forms.ErrorDialogForm(ex, title).ShowDialog(owner);
                        }
                        return;
                    }
                    catch
                    {
                        // fall back to showing without owner
                    }
                }

                // No open forms - show dialog modally
                new Forms.ErrorDialogForm(ex, title).ShowDialog();
            }
            catch
            {
                // Swallow to avoid recursive failures
            }
        }
    }
}