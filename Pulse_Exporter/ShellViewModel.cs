using Caliburn.Micro;
namespace Pulse_Exporter 
{
    public class ShellViewModel : Conductor<object>, IShell 
    {
        public ShellViewModel()
        {
            base.DisplayName = "Pulse Batch Exporter";

            // Set the view
            var evm = IoC.Get<ExporterViewModel>();
            ActivateItem(evm);
        }
    }
}