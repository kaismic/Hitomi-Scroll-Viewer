using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Index))]
    public partial class ImageInfo{
        [JsonIgnore]
        public long Id { get; set; }
        private string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                IsPlayable = value.EndsWith(".gif");
                LocalFileName = IndexFromNameRegex().Match(value).Groups[1].Value;
                Index = int.Parse(LocalFileName);
            }
        }
        [GeneratedRegex("""(\d+).*""")]
        private static partial Regex IndexFromNameRegex();
        public int Index { get; private set; }
        public string LocalFileName { get; private set; }
        public bool IsPlayable { get; private set; }

        private string _imageFilePath;
        public string ImageFilePath {
            get => _imageFilePath ??= Path.Combine(
                Constants.IMAGE_DIR_V3,
                Gallery.Id.ToString(),
                Index.ToString(),
                FileExtension
            );
            set => _imageFilePath = value;
        }

        public string Hash { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public int Haswebp { get; set; }
        public int Hasavif { get; set; }
        public int Hasjxl { get; set; }
        private string _fileFormat;
        public string FileExtension {
            get => _fileFormat ??=
                Haswebp == 1 ? ".webp" :
                Hasavif == 1 ? ".avif" : ".jxl";
        }
        
        [Required]
        public virtual Gallery Gallery { get; set; }
    }
}
