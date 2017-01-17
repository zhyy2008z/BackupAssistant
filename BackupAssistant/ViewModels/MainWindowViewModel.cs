using System;
using System.IO;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Commands;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using BackupAssistant.Utilities;
using BackupAssistant.Models;
using ZetaLongPaths;

namespace BackupAssistant.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        const string Config = "config.json";
        private string _title = "文件备份助手" + Properties.Settings.Default.MyLabel;
        BackupManager manager;

        [JsonIgnore]
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        [JsonIgnore]
        public System.Windows.Window Window { get; set; }

        public MainWindowViewModel()
        {
            AddDirectoryCommand = new DelegateCommand(addDirectoryAction);
            EditDirectoryCommand = new DelegateCommand(editDirectoryAction);
            DeleteDirectoryCommand = new DelegateCommand(deleteDirectoryAction);
            StartBackupCommand = new DelegateCommand(startBackupAction);
            StopBackupCommand = new DelegateCommand(stopBackupAction);
            ClearConsoleCommand = new DelegateCommand(() =>
            {
                AddedInfo.Clear();
                DeletedInfo.Clear();
                ReplacedInfo.Clear();
                LoggedInfo.Clear();
            });
            RebuildSelectedBackToDirectoryMetadataCommand = new DelegateCommand(async () =>
            {
                if (SelectedIndex > -1 && SelectedIndex < this.EditDirectories.Count)
                {
                    var backupTo = this.EditDirectories[SelectedIndex].BackupTo;
                    var metaFile = ZlpPathHelper.Combine(backupTo, BackupManager.MetaData);

                    if (!ZlpIOHelper.FileExists(metaFile) || System.Windows.MessageBox.Show(Window, "检查到元数据文件已存在，请确是否重新生成？", "生成确认", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                    {
                        CanStart = false;
                        Progress = 0;
                        Status = "正在重建元数据中...";
                        await manager.RebuildBackupToMetaData(backupTo);
                        CanStart = true;
                        Status = "重建元数据结束！";
                    }
                }
            });
            StartBackupSelectedCommand = new DelegateCommand(async () =>
            {
                if (SelectedIndex > -1 && SelectedIndex < this.EditDirectories.Count)
                {
                    CanStart = false;
                    CanStop = true;
                    Progress = 0;
                    Status = "正在备份中...";

                    await manager.StartBackup(this, this.SelectedIndex);

                    CanStart = true;
                    CanStop = false;
                    Status = "备份结束！";
                }
            });


            manager = new BackupManager();
            manager.InfoAdded += (s, e) => { Window.Dispatcher.Invoke(() => AddedInfo.Add(e.Info)); };
            manager.InfoDeleted += (s, e) => { Window.Dispatcher.Invoke(() => DeletedInfo.Add(e.Info)); };
            manager.InfoReplaced += (s, e) => { Window.Dispatcher.Invoke(() => ReplacedInfo.Add(e.Info)); };
            manager.InfoLogged += (s, e) => { Window.Dispatcher.Invoke(() => LoggedInfo.Add(e.Info)); };
            manager.ProgressReport += (s, e) => { Window.Dispatcher.Invoke(() => Progress = e.Progress); };
        }

        #region Commands
        [JsonIgnore]
        public DelegateCommand AddDirectoryCommand { get; }
        void addDirectoryAction()
        {
            Views.EditDirectory ed = new Views.EditDirectory();
            ed.Owner = Window;
            ed.Title = "添加";
            ed.ShowDialog();
            if (!string.IsNullOrEmpty(ed.ViewModel.BackupFrom) && !string.IsNullOrEmpty(ed.ViewModel.BackupTo))
            {
                EditDirectories.Add(ed.ViewModel);
            }
        }

        [JsonIgnore]
        public DelegateCommand EditDirectoryCommand { get; }
        void editDirectoryAction()
        {
            if (SelectedIndex > -1 && SelectedIndex < EditDirectories.Count)
            {
                Views.EditDirectory ed = new Views.EditDirectory();
                ed.Owner = Window;
                ed.Title = "修改";
                ed.ViewModel = EditDirectories[SelectedIndex];
                ed.ShowDialog();
            }
        }
        [JsonIgnore]
        public DelegateCommand DeleteDirectoryCommand { get; }

        void deleteDirectoryAction()
        {
            if (SelectedIndex > -1 && SelectedIndex < EditDirectories.Count)
                if (System.Windows.MessageBox.Show(Window, "确定要删除吗？", "确认", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                    EditDirectories.RemoveAt(SelectedIndex);
        }

        [JsonIgnore]
        public DelegateCommand StartBackupCommand { get; }
        async void startBackupAction()
        {
            CanStart = false;
            CanStop = true;
            Progress = 0;
            Status = "正在备份中...";

            await manager.StartBackup(this);

            CanStart = true;
            CanStop = false;
            Status = "备份结束！";
        }

        [JsonIgnore]
        public DelegateCommand StopBackupCommand { get; }
        void stopBackupAction()
        {
            manager.StopBackup();
            Status = "正在取消备份...";
        }

        [JsonIgnore]
        public DelegateCommand ClearConsoleCommand { get; }

        [JsonIgnore]
        public DelegateCommand RebuildSelectedBackToDirectoryMetadataCommand { get; }

        [JsonIgnore]
        public DelegateCommand StartBackupSelectedCommand { get; }

        #endregion

        ObservableCollection<EditDirectoryViewModel> _editDirectories = new ObservableCollection<EditDirectoryViewModel>();
        public ObservableCollection<EditDirectoryViewModel> EditDirectories { get { return _editDirectories; } set { SetProperty(ref _editDirectories, value); } }


        int _selectedIndex;
        public int SelectedIndex { get { return _selectedIndex; } set { SetProperty(ref _selectedIndex, value); } }


        bool _skipSymbolLink = true;
        public bool SkipSymbolLink { get { return _skipSymbolLink; } set { SetProperty(ref _skipSymbolLink, value); } }

        bool _canStart = true;
        [JsonIgnore]
        public bool CanStart { get { return _canStart; } set { SetProperty(ref _canStart, value); } }

        bool _canStop = false;
        [JsonIgnore]
        public bool CanStop { get { return _canStop; } set { SetProperty(ref _canStop, value); } }

        int _progress;
        [JsonIgnore]
        public int Progress { get { return _progress; } set { SetProperty(ref _progress, value); } }

        string _status;
        [JsonIgnore]
        public string Status { get { return _status; } set { SetProperty(ref _status, value); } }

        [JsonIgnore]
        public ObservableCollection<string> AddedInfo { get; set; } = new ObservableCollection<string>();

        [JsonIgnore]
        public ObservableCollection<string> ReplacedInfo { get; set; } = new ObservableCollection<string>();

        [JsonIgnore]
        public ObservableCollection<string> DeletedInfo { get; set; } = new ObservableCollection<string>();

        [JsonIgnore]
        public ObservableCollection<string> LoggedInfo { get; set; } = new ObservableCollection<string>();

        #region 事件方法
        //todo: 这三个方法，本不该这里实现，这里仅是为了证明该技术的可行性
        public async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CanStop)
            {
                if (System.Windows.MessageBox.Show(Window, "我们正在备份你的资料，确认要退出吗？", "退出确认", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.No)
                    e.Cancel = true;
                else
                {
                    e.Cancel = true;
                    await manager.StopBackup();
                    Window.Close();
                }
            }
        }

        public void Window_Closed(object sender, EventArgs e)
        {
            var jsonFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config);
            File.WriteAllText(jsonFileName, JsonConvert.SerializeObject(this));
        }

        public void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var jsonFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config);
            if (File.Exists(jsonFileName))
            {
                var viewModel = JsonConvert.DeserializeObject<MainWindowViewModel>(File.ReadAllText(jsonFileName));
                foreach (var editDirectory in viewModel.EditDirectories)
                {
                    //检测备份到的分区序列号是否与备份到路径有一致的根路径，如果不一致就提示更新
                    if (!string.IsNullOrEmpty(editDirectory.BackupToSerialNumber))
                    {
                        var deviceId = Volumes.GetDeviceId(editDirectory.BackupToSerialNumber);
                        if (!string.IsNullOrEmpty(deviceId) && !editDirectory.BackupTo.StartsWith(deviceId, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (System.Windows.MessageBox.Show(Window, $"检测到备份到路径【{editDirectory.BackupTo}】的根路径应该为【{deviceId + "\\"}】，是否要更新该根路径？", "选择", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                            {
                                editDirectory.BackupTo = deviceId + editDirectory.BackupTo.Substring(2);
                            }
                        }
                    }
                    Window.DataContext = viewModel;
                }
            }
            #endregion
        }
    }
}
