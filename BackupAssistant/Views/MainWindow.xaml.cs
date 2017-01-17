using System.Management;
using System.Windows;
using System.Text;
using System;
using System.Collections.Generic;

namespace BackupAssistant.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public ViewModels.MainWindowViewModel ViewModel { get { return DataContext as ViewModels.MainWindowViewModel; } set { DataContext = value; } }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.Window = this;
        }      
    }
}
