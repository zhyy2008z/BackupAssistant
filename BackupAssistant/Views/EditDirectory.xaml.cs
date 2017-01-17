using System.Windows;
using System.Windows.Forms;
using BackupAssistant.ViewModels;

namespace BackupAssistant.Views
{
    /// <summary>
    /// Interaction logic for EditDirectory.xaml
    /// </summary>
    public partial class EditDirectory : Window
    {
        public EditDirectory()
        {
            InitializeComponent();
        }

        public ViewModels.EditDirectoryViewModel ViewModel { get { return DataContext as ViewModels.EditDirectoryViewModel; } set { DataContext = value; } }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.Window = this;
        }
    }
}
