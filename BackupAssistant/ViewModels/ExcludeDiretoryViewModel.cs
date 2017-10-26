using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace BackupAssistant.ViewModels
{
    /// <summary>
    /// 被排除的文件夹ViewModel
    /// </summary>
    public class ExcludeDiretoryViewModel : BindableBase
    {
        bool _partialPath;
        /// <summary>
        /// 是否部分路径匹配模式
        /// </summary>
        public bool PartialPath { get { return _partialPath; } set { SetProperty(ref _partialPath, value); } }

        string _path;
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get { return _path; } set { SetProperty(ref _path, value); } }
    }
}
