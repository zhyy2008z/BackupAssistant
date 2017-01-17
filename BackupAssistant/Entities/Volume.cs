using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace BackupAssistant.Entities
{
    public class Volume
    {
        public string VolumeSerialNumber { get; set; }

        public string DeviceId { get; set; }
    }
}
