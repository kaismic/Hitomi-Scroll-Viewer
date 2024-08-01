using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitomi_Scroll_Viewer.Entities {
    public class ImageInfo {
        public string Name { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int HasWebp { get; set; }

        public int HasAvif { get; set; }
        public int HasJxl { get; set; }
        public string Hash { get; set; }
    }
}
