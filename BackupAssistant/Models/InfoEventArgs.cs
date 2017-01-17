using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAssistant.Models
{


    public class InfoEventArgs : EventArgs
    {
        public InfoEventArgs(string info)
        {
            this.Info = info;
        }


        public string Info { get; }
    }
}
