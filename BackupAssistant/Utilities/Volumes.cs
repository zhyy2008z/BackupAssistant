using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using BackupAssistant.Entities;

namespace BackupAssistant.Utilities
{
    public static class Volumes
    {
        static List<Volume> volumes;

        static Volumes()
        {
            //收集系统分区信息
            volumes = new List<Volume>();
            using (ManagementClass mc = new ManagementClass("Win32_LogicalDisk"))
            {
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    volumes.Add(new Volume() { VolumeSerialNumber = (string)mo["VolumeSerialNumber"], DeviceId = (string)mo["DeviceId"] });
                }
            }
        }

        public static string GetVolumeSerialNumber(string directoryPath)
        {
            return volumes.SingleOrDefault(vvm => directoryPath.StartsWith(vvm.DeviceId, System.StringComparison.CurrentCultureIgnoreCase))?.VolumeSerialNumber;
        }

        public static string GetDeviceId(string serialNumber)
        {
            return volumes.SingleOrDefault(vvm => vvm.VolumeSerialNumber == serialNumber)?.DeviceId;
        }
    }

}
