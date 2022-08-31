using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Updater.Controller
{
    public static class JsonPropertyHandler
    {
        public static T convertJson<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
        public static void saveJson<T>(T data,string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(data));
        }
    }
}
