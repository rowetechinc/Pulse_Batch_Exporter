using AutoUpdaterDotNET;
using Caliburn.Micro;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Pulse_Exporter 
{
    public class ShellViewModel : Conductor<object>, IShell 
    {
        /// <summary>
        /// Error log path.
        /// </summary>
        public const string ERROR_LOG_PATH = @"C:\RTI_Capture\ExporterErrorLog.log";

        /// <summary>
        /// Initialize.
        /// </summary>
        public ShellViewModel()
        {
            base.DisplayName = "Pulse Batch Exporter";

            // Check Auto Updater
            CheckAutoUpdater();

            // Setup the error log
            SetupErrorLog();

            // Set the view
            var evm = IoC.Get<ExporterViewModel>();
            ActivateItem(evm);
        }

        #region Error Logger

        /// <summary>
        /// Setup the error log.
        /// </summary>
        private void SetupErrorLog()
        {
            bool isAdmin = false;

            Hierarchy hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders(); /*Remove any other appenders*/

            FileAppender fileAppender = new FileAppender();
            fileAppender.AppendToFile = true;
            fileAppender.LockingModel = new FileAppender.MinimalLock();
            fileAppender.File = @"C:\RTI_Capture\ExporterErrorLog.log";
            PatternLayout pl = new PatternLayout();
            string pulseVer = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            //string rtiVer = System.Reflection.Assembly.LoadFrom("RTI.dll").GetName().Version.ToString();
            string rtiVer = "";
            pl.ConversionPattern = "%d [%2%t] %-5p [%-10c] Pulse:" + pulseVer + " RTI:" + rtiVer + "   %m%n%n";
            pl.ActivateOptions();

            // If not Admin
            // Only log Error and Fatal errors
            if (!isAdmin)
            {
                fileAppender.AddFilter(new log4net.Filter.LevelMatchFilter() { LevelToMatch = log4net.Core.Level.Error });          // Log Error
                fileAppender.AddFilter(new log4net.Filter.LevelMatchFilter() { LevelToMatch = log4net.Core.Level.Fatal });          // Log Fatal
                fileAppender.AddFilter(new log4net.Filter.DenyAllFilter());                                                         // Reject all other errors
            }

            fileAppender.Layout = pl;
            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(fileAppender);
        }

        /// <summary>
        /// Clear the Error Log.
        /// </summary>
        public void ClearErrorLog()
        {
            using (FileStream stream = new FileStream(ERROR_LOG_PATH, FileMode.Create))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("");
                }
            }
        }

        #endregion

        #region Auto Updater

        /// <summary>
        /// Check for the latest version of the exporter.
        /// </summary>
        private void CheckAutoUpdater()
        {
            AutoUpdater.Start("http://www.rowetechinc.com/swfw/latest/Exporter/Pulse_Exporter_AppCast.xml");
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
        }



        private void AutoUpdater_ApplicationExitEvent()
        {
            //Text = @"Closing application...";
            //Thread.Sleep(1000);
            Application.Exit();
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    DialogResult dialogResult;
                    if (args.Mandatory)
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.  You may have to close the current version to complete the install process.", @"Update Available",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Information);
                    }
                    else
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {
                                        args.InstalledVersion
                                    }. Do you want to update the application now?", @"Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);
                    }

                    // Uncomment the following line if you want to show standard update dialog instead.
                    // AutoUpdater.ShowUpdateForm();

                    if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate())
                            {
                                Application.Exit();
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    //MessageBox.Show(@"There is no update available please try again later.", @"No update available",
                    //    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(
                        @"There is a problem reaching update server please check your internet connection and try again later.",
                        @"Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}