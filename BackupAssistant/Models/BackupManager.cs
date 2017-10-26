using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using Newtonsoft.Json;
using BackupAssistant.Entities;
using System.Collections.Specialized;
using Microsoft.VisualBasic.FileIO;
using ZetaLongPaths;
using ZetaLongPaths.Native;

namespace BackupAssistant.Models
{
    using Utilities;
    using ViewModels;

    /// <summary>
    /// 备份功能实现类
    /// </summary>
    public class BackupManager : Component
    {
        Task _task;
        CancellationTokenSource _source;
        public const string MetaData = "_metadata.json";
        int _baseProgress;
        long _currentTaskBackupFileCount;
        int _everyTaskTotalProgress;
        long _currentTaskTotalFileCount;
        bool _skipSymbolLink;
        /// <summary>
        /// 仅备份指定索引号的目录
        /// </summary>
        int? _backupIndex;
        CancellationToken _token;
        int _fileAdded, _fileUpdated, _fileDeleted, _folderAdded, _folderDeleted;

        public BackupManager() { }

        public BackupManager(Container container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            container.Add(this);
        }

        public Task StartBackup(ViewModels.MainWindowViewModel viewModel, int? backupIndex = null)
        {
            _source = new CancellationTokenSource();
            this._backupIndex = backupIndex;
            return _task = Task.Factory.StartNew(backupAction, viewModel, _source.Token);
        }

        public Task RebuildBackupToMetaData(string backupToDirectory)
        {
            return Task.Run(() =>
            {
                try
                {
                    var metaFile = ZlpPathHelper.Combine(backupToDirectory, MetaData);

                    OnInfoLogged(new InfoEventArgs($"正在分析备份到文件夹【{backupToDirectory}】元数据"));
                    var metaData = analysisBackupTo(backupToDirectory);
                    OnInfoLogged(new InfoEventArgs($"分析备份到文件夹【{backupToDirectory}】元数据完成"));

                    OnInfoLogged(new InfoEventArgs($"正在保存备份到文件夹【{backupToDirectory}】元数据"));
                    ZlpIOHelper.WriteAllText(metaFile, JsonConvert.SerializeObject(metaData));
                    OnInfoLogged(new InfoEventArgs($"保存备份到文件夹【{backupToDirectory}】元数据完成"));

                    OnProgressReport(new ProgressReportEventArgs(100));
                }
                catch (Exception ex)
                {
                    logError($"重建备份到文件夹【{backupToDirectory}】元数据失败", ex);
                }
            });
        }

        private void backupAction(object state)
        {
            using (_source)
            {
                try
                {
                    _token = _source.Token;
                    var viewModel = state as ViewModels.MainWindowViewModel;
                    _baseProgress = 0;
                    _skipSymbolLink = viewModel.SkipSymbolLink;
                    _fileAdded = _fileUpdated = _fileDeleted = _folderAdded = _folderDeleted = 0;

                    if (_backupIndex.HasValue)
                    {
                        _everyTaskTotalProgress = 100;
                        backupFolderToFolder(viewModel.EditDirectories[_backupIndex.Value]);
                    }
                    else
                    {
                        if (viewModel.EditDirectories.Count != 0)
                            _everyTaskTotalProgress = 100 / viewModel.EditDirectories.Count;

                        foreach (var item in viewModel.EditDirectories)
                        {
                            if (_token.IsCancellationRequested)
                                break;

                            backupFolderToFolder(item);
                            _baseProgress += _everyTaskTotalProgress;
                            OnProgressReport(new ProgressReportEventArgs(_baseProgress));
                        }
                    }

                    if (_token.IsCancellationRequested)
                    {
                        OnInfoLogged(new Models.InfoEventArgs("备份操作已取消"));
                    }
                    else
                    {
                        OnProgressReport(new ProgressReportEventArgs(100));
                        OnInfoLogged(new InfoEventArgs("恭喜，所有选定的文件夹已备份完成"));
                        OnInfoLogged(new InfoEventArgs($"本次共计新增文件 {_fileAdded} 个，更新文件 {_fileUpdated} 个，删除文件 {_fileDeleted} 个，创建文件夹 {_folderAdded} 个，删除文件夹 {_folderDeleted} 个"));
                    }
                }
                catch (Exception ex)
                {
                    OnInfoLogged(new InfoEventArgs($"【未预料到的错误】{ex.Message}"));
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"{DateTime.Now} \t {ex}");
                    sb.AppendLine();

                    ZlpIOHelper.AppendText(ZlpPathHelper.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"), sb.ToString());
                }
            }
        }

        void logError(string message, Exception ex)
        {
            OnInfoLogged(new InfoEventArgs("【错误】" + message + "【Exception】" + ex.Message));
        }

        /// <summary>
        /// 配置一个备份设置条目，并启动备份过程
        /// </summary>
        /// <param name="viewModel"></param>
        void backupFolderToFolder(EditDirectoryViewModel viewModel)
        {
            _currentTaskBackupFileCount = 0;
            //分析备份到文件夹当前文件情况
            if (!ZlpIOHelper.DirectoryExists(viewModel.BackupFrom))
            {
                OnInfoLogged(new InfoEventArgs($"备份自【{viewModel.BackupFrom}】文件夹不存在，已跳过"));
                return;
            }

            var backupFromDirectory = new ZlpDirectoryInfo(viewModel.BackupFrom);

            //备份自是符号链接
            if (_skipSymbolLink && backupFromDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint) && SymbolicLink.GetTarget(backupFromDirectory.FullName) != null)
            {
                OnInfoLogged(new InfoEventArgs($"备份自【{viewModel.BackupFrom}】是符号链接，已跳过"));
                return;
            }

            //检验文件夹权限
            ZlpFileInfo[] innerFiles;
            try
            {
                innerFiles = backupFromDirectory.GetFiles();
            }
            catch (Exception ex)
            {
                logError($"获取备份自文件夹【{backupFromDirectory.FullName}】文件列表出错", ex);
                return;
            }

            if (!ZlpIOHelper.DirectoryExists(viewModel.BackupTo))
            {
                if (!ZlpIOHelper.DirectoryExists(ZlpPathHelper.GetDirectoryPathNameFromFilePath(viewModel.BackupTo)))
                {
                    OnInfoLogged(new InfoEventArgs($"【错误】备份到【{viewModel.BackupTo}】的上级目录不存在"));
                    return;
                }
                else
                {
                    try
                    {
                        ZlpIOHelper.CreateDirectory(viewModel.BackupTo);
                        OnInfoAdded(new InfoEventArgs($"创建备份到【{viewModel.BackupTo}】文件夹"));
                    }
                    catch (Exception ex)
                    {
                        logError($"创建备份到【{viewModel.BackupTo}】文件夹失败", ex);
                        return;
                    }
                }
            }

            var metaFile = ZlpPathHelper.Combine(viewModel.BackupTo, MetaData);
            MyDirectory metaData;
            if (ZlpIOHelper.FileExists(metaFile))
            {
                OnInfoLogged(new InfoEventArgs($"正在加载备份到文件夹【{viewModel.BackupTo}】元数据"));
                //读取元数据
                metaData = JsonConvert.DeserializeObject<MyDirectory>(ZlpIOHelper.ReadAllText(metaFile));
                OnInfoLogged(new InfoEventArgs($"加载备份到文件夹【{viewModel.BackupTo}】元数据完成"));
            }
            else
            {
                OnInfoLogged(new InfoEventArgs($"正在分析备份到文件夹【{viewModel.BackupTo}】元数据"));
                metaData = analysisBackupTo(viewModel.BackupTo);
                OnInfoLogged(new InfoEventArgs($"分析备份到文件夹【{viewModel.BackupTo}】元数据完成"));
            }

            List<string> excludeDirectories = null;
            List<string> excludePartialPaths = null;
            foreach (var item in viewModel.DirectoryExcludes)
            {
                if (item.PartialPath)
                {
                    if (System.IO.Path.GetFileName(viewModel.BackupFrom).Equals(item.Path, StringComparison.CurrentCultureIgnoreCase))
                    {
                        OnInfoLogged(new InfoEventArgs($"备份自【{viewModel.BackupFrom}】在排除列表中，已跳过"));
                        return;
                    }
                    else
                    {
                        if (excludePartialPaths == null)
                            excludePartialPaths = new List<string>();
                        excludePartialPaths.Add(item.Path);
                    }
                }
                else
                {
                    if (item.Path.Equals(viewModel.BackupFrom, StringComparison.CurrentCultureIgnoreCase))
                    {
                        OnInfoLogged(new InfoEventArgs($"备份自【{viewModel.BackupFrom}】在排除列表中，已跳过"));
                        return;
                    }
                    else if (item.Path.StartsWith(viewModel.BackupFrom, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (excludeDirectories == null)
                            excludeDirectories = new List<string>();
                        excludeDirectories.Add(item.Path);
                    }
                }
            }

            OnInfoLogged(new InfoEventArgs($"正在统计【{viewModel.BackupFrom}】需要备份文件数量"));
            //计算总计需要备份的文件数量
            _currentTaskTotalFileCount = 0;
            calcTaskTotalFileCount(new ZlpDirectoryInfo(viewModel.BackupFrom)); //计算结果保存在 currentTaskTotalFileCount 中
            OnInfoLogged(new InfoEventArgs($"统计完成，【{viewModel.BackupFrom}】需要备份文件 {_currentTaskTotalFileCount} 个"));

            OnInfoLogged(new InfoEventArgs($"正在备份【{viewModel.BackupFrom}】中..."));

            backup(backupFromDirectory, metaData, viewModel.BackupTo, innerFiles, excludeDirectories, excludePartialPaths);

            //保存文件元数据
            OnInfoLogged(new InfoEventArgs($"正在保存备份到文件夹【{viewModel.BackupTo}】元数据"));
            ZlpIOHelper.WriteAllText(metaFile, JsonConvert.SerializeObject(metaData));
            OnInfoLogged(new InfoEventArgs($"保存备份到文件夹【{viewModel.BackupTo}】元数据完成"));

            //通知备份结果
            OnInfoLogged(new InfoEventArgs($"备份文件夹【{viewModel.BackupFrom}】{(_token.IsCancellationRequested ? "已取消" : "完成")}"));
        }

        void calcTaskTotalFileCount(ZlpDirectoryInfo directoryInfo)
        {
            ZlpFileInfo[] files;
            try
            {
                files = directoryInfo.GetFiles();
            }
            catch (Exception ex)
            {
                logError($"统计备份文件夹【{directoryInfo.FullName}】出错", ex);
                return;
            }

            _currentTaskTotalFileCount += files.Length;

            foreach (var di in directoryInfo.GetDirectories())
                if (!_skipSymbolLink || !(di.Attributes.HasFlag(FileAttributes.ReparsePoint) && SymbolicLink.GetTarget(di.FullName) != null))
                    calcTaskTotalFileCount(di);

        }

        private void backup(ZlpDirectoryInfo directoryInfo, MyDirectory myDirectory, string containerDir, ZlpFileInfo[] files, List<string> excludeDirectories, List<string> excludePartialPaths)
        {
            //备份文件
            List<string> fileNames = new List<string>();

            foreach (var file in files)
            {
                var bkToFile = myDirectory.Files.SingleOrDefault(mf => mf.Name.Equals(file.Name, StringComparison.CurrentCultureIgnoreCase));
                if (bkToFile == null) //文件新增
                {
                    try
                    {
                        var fullName = ZlpPathHelper.Combine(containerDir, file.Name);
                        file.CopyTo(fullName, false);
                        bkToFile = new MyFile() { LastWriteTime = file.LastWriteTime, Length = file.Length, Name = file.Name };
                        myDirectory.Files.Add(bkToFile);
                        OnInfoAdded(new InfoEventArgs("【文件】" + fullName));
                        _fileAdded++;
                    }
                    catch (Exception ex)
                    {
                        logError($"添加文件【{file.FullName}】失败", ex);
                    }

                }
                else if (bkToFile.Length != file.Length || bkToFile.LastWriteTime != file.LastWriteTime) // 文件已修改
                {
                    try
                    {
                        var fullName = ZlpPathHelper.Combine(containerDir, file.Name);
                        //替换操作
                        FileSystem.DeleteFile(fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin); //先删除
                        file.CopyTo(fullName, false);//再复制
                        bkToFile.LastWriteTime = file.LastWriteTime;
                        bkToFile.Length = file.Length;
                        bkToFile.Name = file.Name;
                        OnInfoReplaced(new InfoEventArgs("【文件】" + fullName));
                        _fileUpdated++;
                    }
                    catch (Exception ex)
                    {
                        logError($"更新文件【{file.FullName}】失败", ex);
                    }
                }

                fileNames.Add(file.Name);

                //用户取消备份
                if (_token.IsCancellationRequested)
                    return;
            }

            //评估删除文件
            for (int i = myDirectory.Files.Count - 1; i > -1; i--)
            {
                if (!fileNames.Exists(s => s.Equals(myDirectory.Files[i].Name, StringComparison.CurrentCultureIgnoreCase)))//文件已删除
                {
                    //删除文件并发送到回收站
                    var fullName = ZlpPathHelper.Combine(containerDir, myDirectory.Files[i].Name);
                    FileSystem.DeleteFile(fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    myDirectory.Files.RemoveAt(i);
                    OnInfoDeleted(new InfoEventArgs("【文件】" + fullName));
                    _fileDeleted++;
                }
            }

            //报告进度
            _currentTaskBackupFileCount += fileNames.Count;
            OnProgressReport(new ProgressReportEventArgs(_baseProgress + (int)((_currentTaskBackupFileCount * _everyTaskTotalProgress) / _currentTaskTotalFileCount)));

            //初始化文件夹
            List<string> directoryNames = new List<string>();
            foreach (var directory in directoryInfo.GetDirectories())
            {
                //跳过符号链接
                if (_skipSymbolLink && directory.Attributes.HasFlag(FileAttributes.ReparsePoint) && SymbolicLink.GetTarget(directory.FullName) != null)
                {
                    OnInfoLogged(new InfoEventArgs($"已跳过符号链接【{directory.FullName}】"));
                    continue;
                }

                //判断部分匹配排除列表
                if (excludePartialPaths != null)
                    foreach (var item in excludePartialPaths)
                    {
                        if (item.Equals(directory.Name, StringComparison.CurrentCultureIgnoreCase))
                        {
                            OnInfoLogged(new InfoEventArgs($"备份文件夹【{directory.FullName}】在排除列表中，已跳过"));
                            goto outer;
                        }
                    }

                List<string> subExcludeDirectories = null;
                //判断排除列表
                if (excludeDirectories != null)
                    foreach (var item in excludeDirectories)
                        if (item.Equals(directory.FullName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            OnInfoLogged(new InfoEventArgs($"备份文件夹【{directory.FullName}】在排除列表中，已跳过"));
                            goto outer;
                        }
                        else if (item.StartsWith(directory.FullName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (subExcludeDirectories == null)
                                subExcludeDirectories = new List<string>();
                            subExcludeDirectories.Add(item);
                        }

                //检验文件夹权限
                ZlpFileInfo[] innerFiles;
                try
                {
                    innerFiles = directory.GetFiles();
                }
                catch (Exception ex)
                {
                    logError($"备份文件夹【{directoryInfo.FullName}】出错", ex);
                    continue;
                }

                //收集当前文件夹信息
                var bkToDirectory = myDirectory.Directories.SingleOrDefault(mf => mf.Name.Equals(directory.Name, StringComparison.CurrentCultureIgnoreCase));
                if (bkToDirectory == null) //需要新增文件夹
                {
                    var fullName = ZlpPathHelper.Combine(containerDir, directory.Name);
                    ZlpIOHelper.CreateDirectory(fullName);
                    bkToDirectory = new MyDirectory() { Name = directory.Name };
                    myDirectory.Directories.Add(bkToDirectory);
                    OnInfoAdded(new InfoEventArgs("【文件夹】" + fullName));
                    _folderAdded++;
                }

                directoryNames.Add(directory.Name);

                backup(directory, bkToDirectory, ZlpPathHelper.Combine(containerDir, bkToDirectory.Name), innerFiles, subExcludeDirectories, excludePartialPaths);

                outer:; //用于跳出本次循环
            }

            //评估删除文件夹
            for (int i = myDirectory.Directories.Count - 1; i > -1; i--)
            {
                if (!directoryNames.Exists(s => s.Equals(myDirectory.Directories[i].Name, StringComparison.CurrentCultureIgnoreCase)))//文件已删除
                {
                    //删除文件并发送到回收站
                    var fullName = ZlpPathHelper.Combine(containerDir, myDirectory.Directories[i].Name);
                    FileSystem.DeleteDirectory(fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    myDirectory.Directories.RemoveAt(i);
                    OnInfoDeleted(new InfoEventArgs("【文件夹】" + fullName));
                    _folderDeleted++;
                }
            }
        }

        private MyDirectory analysisBackupTo(string backupTo)
        {
            MyDirectory metaData = new Entities.MyDirectory();

            analysis(new ZlpDirectoryInfo(backupTo), metaData);
            var metaFile = metaData.Files.SingleOrDefault(mf => mf.Name.Equals(MetaData, StringComparison.CurrentCultureIgnoreCase));

            if (metaFile != null) //删除元数据文件
                metaData.Files.Remove(metaFile);

            return metaData;
        }


        void analysis(ZlpDirectoryInfo directoryInfo, MyDirectory directory)
        {
            directory.Name = directoryInfo.Name;

            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                directory.Files.Add(new MyFile() { Length = fileInfo.Length, LastWriteTime = fileInfo.LastWriteTime, Name = fileInfo.Name });
            }

            foreach (var di in directoryInfo.GetDirectories())
            {
                var md = new MyDirectory();
                directory.Directories.Add(md);
                analysis(di, md);
            }
        }

        public Task StopBackup()
        {
            _source?.Cancel();
            return _task;
        }


        static readonly object InfoAddedEvent = new object();
        public event EventHandler<InfoEventArgs> InfoAdded { add { this.Events.AddHandler(InfoAddedEvent, value); } remove { this.Events.RemoveHandler(InfoAddedEvent, value); } }
        protected virtual void OnInfoAdded(InfoEventArgs e) => ((EventHandler<InfoEventArgs>)Events[InfoAddedEvent])?.Invoke(this, e);


        static readonly object InfoReplacedEvent = new object();
        public event EventHandler<InfoEventArgs> InfoReplaced { add { Events.AddHandler(InfoReplacedEvent, value); } remove { Events.RemoveHandler(InfoReplacedEvent, value); } }
        protected virtual void OnInfoReplaced(InfoEventArgs e) => ((EventHandler<InfoEventArgs>)Events[InfoReplacedEvent])?.Invoke(this, e);

        static readonly object InfoDeletedEvent = new object();
        public event EventHandler<InfoEventArgs> InfoDeleted { add { Events.AddHandler(InfoDeletedEvent, value); } remove { Events.RemoveHandler(InfoDeletedEvent, value); } }
        protected virtual void OnInfoDeleted(InfoEventArgs e) => ((EventHandler<InfoEventArgs>)Events[InfoDeletedEvent])?.Invoke(this, e);

        static readonly object InfoLoggedEvent = new object();
        public event EventHandler<InfoEventArgs> InfoLogged { add { Events.AddHandler(InfoLoggedEvent, value); } remove { Events.RemoveHandler(InfoLoggedEvent, value); } }
        protected virtual void OnInfoLogged(InfoEventArgs e) => ((EventHandler<InfoEventArgs>)Events[InfoLoggedEvent])?.Invoke(this, e);

        static readonly object ProgressReportEvent = new object();
        public event EventHandler<ProgressReportEventArgs> ProgressReport { add { Events.AddHandler(ProgressReportEvent, value); } remove { Events.RemoveHandler(ProgressReportEvent, value); } }
        protected virtual void OnProgressReport(ProgressReportEventArgs e) => ((EventHandler<ProgressReportEventArgs>)Events[ProgressReportEvent])?.Invoke(this, e);
    }
}
