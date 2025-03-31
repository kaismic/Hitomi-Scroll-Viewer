using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Index))]
    public class GalleryImage {
        public long Id { get; set; }
        public int Index { get; set; }
        public bool IsPlayable { get; set; }
        public required string Hash { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public required string FullFileName { get; set; }
        public required string FileExtension { get; set; }
        [Required] public Gallery Gallery { get; set; } = default!;
    }
}
