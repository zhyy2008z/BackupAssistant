using Microsoft.Practices.Unity;
using Prism.Unity;
using BackupAssistant.Views;
using System.Windows;
using Prism.Regions;

namespace BackupAssistant
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();


        }
    }
}
