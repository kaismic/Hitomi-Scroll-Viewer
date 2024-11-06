using HitomiScrollViewerLib.Entities;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerLib.DTOs {
    public partial class OriginalImageInfoDTO {
        public string Name { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Hash { get; set; }
        public int Hasavif { get; set; }
        public int Hasjxl { get; set; }
        public int Haswebp { get; set; }

        public ImageInfo ToImageInfo() {
            return new() {
                IsPlayable = Name.EndsWith(".gif"),
                Height = Height,
                Width = Width,
                Hash = Hash,
                FileExtension = Haswebp == 1 ? "webp" : Hasavif == 1 ? "avif" : "jxl",
            };
        }
    }
}
