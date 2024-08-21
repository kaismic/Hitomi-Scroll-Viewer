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
        public string Name { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int Haswebp { get; set; }

        public int Hasavif { get; set; }
        public int Hasjxl { get; set; }
        public string Hash { get; set; }
        public virtual Gallery Gallery { get; set; }
    }
}
