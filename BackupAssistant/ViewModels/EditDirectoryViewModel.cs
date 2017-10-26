using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace BackupAssistant.ViewModels
{
    /// <summary>
    /// 编辑备份文件夹ViewModel
    /// </summary>
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
            if (PartialPath && !string.IsNullOrEmpty(ExcludePath))
                DirectoryExcludes.Add(new ExcludeDiretoryViewModel() { Path = ExcludePath, PartialPath = true });
            else
                using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    if (fbd.ShowDialog(new Win32Window(Window)) == System.Windows.Forms.DialogResult.OK)
                    {
                        DirectoryExcludes.Add(new ExcludeDiretoryViewModel() { Path = fbd.SelectedPath, PartialPath = false });
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


        ObservableCollection<ExcludeDiretoryViewModel> _directoryExcludes = new ObservableCollection<ExcludeDiretoryViewModel>();
        public ObservableCollection<ExcludeDiretoryViewModel> DirectoryExcludes { get { return _directoryExcludes; } set { SetProperty(ref _directoryExcludes, value); } }

        string _title;
        [JsonIgnore]
        public string Title { get { return _title; } set { SetProperty(ref _title, value); } }

        string _excludePath;
        public string ExcludePath { get { return _excludePath; } set { SetProperty(ref _excludePath, value); } }

        bool _partialPath;
        public bool PartialPath { get { return _partialPath; } set { SetProperty(ref _partialPath, value); } }
    }
}
