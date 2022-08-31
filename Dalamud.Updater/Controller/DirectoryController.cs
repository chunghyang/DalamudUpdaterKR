using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Updater.Controller
{
    public static class DirectoryController
    {

        public static DirectoryInfo getDirectory(DirectoryInfo info)
        {
            if (!info.Exists) return null;

            var data =info.GetDirectories().ToList().Where(wh => wh.Name != "dev").ToList().OrderByDescending(wh => wh.LastAccessTime).FirstOrDefault();
            return data;
        }

    }
}
