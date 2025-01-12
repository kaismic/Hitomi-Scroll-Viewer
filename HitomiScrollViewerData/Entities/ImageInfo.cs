using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Index))]
    public class ImageInfo {
        public long Id { get; set; }
        public int Index { get; set; }
        public string FileName { get; set; }
        private string _fullFileName;
        public string FullFileName => _fullFileName ??= FileName + '.' + FileExtension;
        public bool IsPlayable { get; set; }
        //public string ImageFilePath => Path.Combine(Constants.IMAGE_DIR_V3, Gallery.Id.ToString(), FullFileName);
        public string Hash { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string FileExtension { get; set; }

        [Required]
        public Gallery Gallery { get; set; }

        public ImageInfoSyncDTO ToImageInfoSyncDTO() => new() {
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
