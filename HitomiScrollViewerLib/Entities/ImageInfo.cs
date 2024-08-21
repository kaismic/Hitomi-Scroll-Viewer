using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.Entities {
    public class ImageInfo {
        [JsonIgnore]
        public long Id { get; set; }
        [JsonIgnore]
        public virtual Gallery Gallery { get; set; }
        public string Name { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int HasWebp { get; set; }

        public int HasAvif { get; set; }
        public int HasJxl { get; set; }
        public string Hash { get; set; }
    }
}
