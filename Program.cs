using System;
using System.Windows.Forms;
using InventoryPOS.Services;

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
            var logger = LoggerService.Instance;
            logger.LogInfo("=== InventoryPOS starting ===");
            logger.LogInfo($"OS: {Environment.OSVersion} | CLR: {Environment.Version} | Working Set: {Environment.WorkingSet}");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Catch unhandled exceptions on UI thread
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                logger.LogError("Unhandled UI Exception", e.Exception);
                try { ShowException(e.Exception, "Unhandled UI Exception"); }
                catch (Exception ex) { logger.LogError("Failed to show error dialog for UI exception", ex); }
            };

            // Catch non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception ?? new Exception(e.ToString());
                logger.LogCritical("Unhandled Thread Exception", ex);
                try { ShowException(ex, "Unhandled Thread Exception"); }
                catch (Exception showEx) { logger.LogError("Failed to show error dialog for thread exception", showEx); }
            };

            // Task scheduler exceptions
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                logger.LogError("Unobserved Task Exception", e.Exception);
                try
                {
                    ShowException(e.Exception, "Unobserved Task Exception");
                    e.SetObserved();
                }
                catch (Exception showEx)
                {
                    logger.LogError("Failed to show error dialog for task exception", showEx);
                }
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                logger.LogCritical("Fatal exception during Application.Run", ex);
                ShowException(ex, "Fatal Application Error");
            }
            finally
            {
                logger.LogInfo("=== InventoryPOS shutting down ===");
                logger.Flush();
            }
        }

        internal static void ShowException(Exception ex, string title)
        {
            try
            {
                // If we have an open UI thread, invoke the dialog there
                if (Application.OpenForms != null && Application.OpenForms.Count > 0)
                {
                    var owner = Application.OpenForms[0];
                    if (owner != null)
                    {
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
