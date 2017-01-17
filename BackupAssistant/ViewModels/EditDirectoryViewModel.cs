using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace BackupAssistant.ViewModels
{
    public class EditDirectoryViewModel : BindableBase
    {
        public EditDirectoryViewModel()
        {
            AddExcludeDirectoryCommand = new DelegateCommand(addExcludeDirectoryAction);
            DeleteExcludeDirectoryCommand = new DelegateCommand(deleteExcludeDirectoryAction);
        }


        #region Commands
        [JsonIgnore]
        public DelegateCommand AddExcludeDirectoryCommand { get; }
        void addExcludeDirectoryAction()
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (fbd.ShowDialog(new Win32Window(Window)) == System.Windows.Forms.DialogResult.OK)
                {
                    DirectoryExcludes.Add(fbd.SelectedPath);
                }
            }
        }

        [JsonIgnore]
        public DelegateCommand DeleteExcludeDirectoryCommand { get; }
        void deleteExcludeDirectoryAction()
        {

            if (SelectedIndex > -1 && SelectedIndex < DirectoryExcludes.Count)
                if (System.Windows.MessageBox.Show(Window, "确定要删除吗？", "确认", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                {
                    DirectoryExcludes.RemoveAt(SelectedIndex);
                }
        }

        #endregion
        [JsonIgnore]
        public System.Windows.Window Window { get; set; }

        string _backupFrom;
        public string BackupFrom
        {
            get { return _backupFrom; }
            set
            {
                SetProperty(ref _backupFrom, value);
            }
        }

        string _backupFromSerialNumber;
        public string BackupFromSerialNumber { get { return _backupFromSerialNumber; } set { SetProperty(ref _backupFromSerialNumber, value); } }
        
        string _backupTo;
        public string BackupTo
        {
            get { return _backupTo; }
            set
            {
                SetProperty(ref _backupTo, value);
            }
        }

        string _backupToSerialNumber;
        public string BackupToSerialNumber { get { return _backupToSerialNumber; } set { SetProperty(ref _backupToSerialNumber, value); } }

        int _selectedIndex;
        public int SelectedIndex { get { return _selectedIndex; } set { SetProperty(ref _selectedIndex, value); } }


        ObservableCollection<string> _directoryExcludes = new ObservableCollection<string>();
        public ObservableCollection<string> DirectoryExcludes { get { return _directoryExcludes; } set { SetProperty(ref _directoryExcludes, value); } }

        string _title;
        [JsonIgnore]
        public string Title { get { return _title; } set { SetProperty(ref _title, value); } }
    }
}
