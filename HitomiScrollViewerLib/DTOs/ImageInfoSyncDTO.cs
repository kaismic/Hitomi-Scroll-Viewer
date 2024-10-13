using HitomiScrollViewerLib.Entities;

namespace HitomiScrollViewerLib.DTOs {
    public class ImageInfoSyncDTO {
        public int Index { get; set; }
        public string FileName { get; set; }
        public bool IsPlayable { get; set; }
        public string Hash { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string FileExtension { get; set; }
        public ImageInfo ToImageInfo() => new() {
            Index = Index,
            FileName = FileName,
            Hash = Hash,
            Height = Height,
            Width = Width,
            FileExtension = FileExtension,
            IsPlayable = IsPlayable
        };
    }
}
