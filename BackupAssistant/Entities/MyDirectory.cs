using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAssistant.Entities
{
    public class MyDirectory:MyFileInfo
    {
        public List<MyDirectory> Directories { get; } = new List<MyDirectory>();

        public List<MyFile> Files { get; } = new List<MyFile>();
    }
}
