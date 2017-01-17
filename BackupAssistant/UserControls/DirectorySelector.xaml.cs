using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BackupAssistant.UserControls
{
    /// <summary>
    /// Interaction logic for FileSelector.xaml
    /// </summary>
    public partial class DirectorySelector : UserControl
    {
        public DirectorySelector()
        {
            InitializeComponent();
        }

        public string SelectedDirectory
        {
            get { return (string)GetValue(SelectedDirectoryProperty); }
            set { SetValue(SelectedDirectoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectDirectory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDirectoryProperty = DependencyProperty.Register(nameof(SelectedDirectory), typeof(string), typeof(DirectorySelector), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));



        public string SerialNumber
        {
            get { return (string)GetValue(SerialNumberProperty); }
            set { SetValue(SerialNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNumberProperty = DependencyProperty.Register("SerialNumber", typeof(string), typeof(DirectorySelector), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        private void BExplore_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {

                fbd.SelectedPath = SelectedDirectory;
                if (fbd.ShowDialog(new Win32Window(Window.GetWindow(this))) == System.Windows.Forms.DialogResult.OK)
                {
                    SelectedDirectory = fbd.SelectedPath;
                    SerialNumber = Utilities.Volumes.GetVolumeSerialNumber(SelectedDirectory);
                }
            }

        }
    }
}
