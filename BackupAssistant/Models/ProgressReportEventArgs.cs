using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAssistant.Models
{
    public class ProgressReportEventArgs : EventArgs
    {
        public ProgressReportEventArgs(int progress)
        {
            this.Progress = progress;
        }

        public int Progress { get; }
    }

}
