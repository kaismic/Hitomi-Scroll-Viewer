using System.Text.RegularExpressions;

namespace HitomiScrollViewerLib.Entities {
    public partial class OriginalImageInfo {
        [GeneratedRegex("""(\d+).*""")]
        private static partial Regex IndexFromNameRegex();

        public string Name { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Hash { get; set; }
        public int Haswebp { get; set; }
        public int Hasavif { get; set; }
        public int Hasjxl { get; set; }

        public ImageInfo ToImageInfo() {
            string fileName = IndexFromNameRegex().Match(Name).Groups[1].Value;
            return new() {
                Index = int.Parse(fileName),
                FileName = fileName,
                IsPlayable = Name.EndsWith(".gif"),
                Height = Height,
                Width = Width,
                Hash = Hash,
                FileExtension = Haswebp == 1 ? "webp" : Hasavif == 1 ? "avif" : "jxl",
            };
        }
    }
}
