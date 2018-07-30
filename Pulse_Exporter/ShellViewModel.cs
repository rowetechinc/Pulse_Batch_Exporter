using Caliburn.Micro;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.IO;

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
    }
}