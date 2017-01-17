using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAssistant.Entities
{
    public class MyFile : MyFileInfo
    {
        public long Length { get; set; }

        public DateTime LastWriteTime { get; set; }
    }
}
