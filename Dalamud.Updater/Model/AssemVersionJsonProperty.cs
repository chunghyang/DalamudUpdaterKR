using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Updater.Model
{
    public class AssemVersionJsonProperty
    {
        [JsonProperty("AssemblyVersion")]
        public string AssemblyVersion { get; set; }

        [JsonProperty("SupportedGameVer")]
        public string SupportedGameVer { get; set; }

        [JsonProperty("RuntimeVersion")]
        public string RuntimeVersion { get; set; }

        [JsonProperty("RuntimeRequired")]
        public bool RuntimeRequired { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("DownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("DotnetUrl")]
        public string DotnetUrl { get; set; }

        [JsonProperty("DesktopUrl")]
        public string DesktopUrl { get; set; }
    }
}
