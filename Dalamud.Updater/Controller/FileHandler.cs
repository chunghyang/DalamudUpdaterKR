using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Updater.Controller
{
    public static class FileHandler
    {
        public static string read(DirectoryInfo info,string fileName)
        {
            string path = Path.Combine(info.FullName, fileName);
            if (!File.Exists(path)) throw new Exception("Addon File이 손상되었습니다 확인해주세요");
            return File.ReadAllText(path);
        }
    }
}
