using HitomiScrollViewerData.Entities;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerData.DTOs {
    public partial class OriginalImageInfoDTO {
        public required string Name { get; set; }
        public required int Height { get; set; }
        public required int Width { get; set; }
        public required string Hash { get; set; }
        public required int Hasavif { get; set; }
        public required int Hasjxl { get; set; }
        public required int Haswebp { get; set; }

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
