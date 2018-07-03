using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_Exporter
{
    using log4net;
    using System.Windows;

    public partial class App : Application
    {
        // Setup logger
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is called when an unhandled exception is called.  This will display a message box, then close the application.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="args">Get the exception message.</param>
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
        {
            // Log the error
            log.Fatal("An unexpected application exception occurred", args.Exception);

            // Display a message box
            // If the user presses OK, the application will shutdown.
            // If the user presses Cancel, the application will continue to run.
            MessageBoxResult result = MessageBox.Show("An unexpected exception has occurred. Shutting down the application. Please check the log file for more details." + args.Exception, "Exception", MessageBoxButton.OKCancel);

            // Prevent default unhandled exception processing
            args.Handled = true;

            // If you press cancel, the program will continue to try and run.
            if (result == MessageBoxResult.OK)
            {
                System.Environment.Exit(0);
            }
        }
    }
}
