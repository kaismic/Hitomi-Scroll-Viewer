using System.Text.Json.Serialization;

namespace HitomiScrollViewerLib.Entities {
    public class ImageInfo {
        [JsonIgnore]
        public long Id { get; set; }
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
