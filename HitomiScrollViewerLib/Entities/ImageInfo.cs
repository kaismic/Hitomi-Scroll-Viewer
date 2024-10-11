using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Index))]
    public partial class ImageInfo {
        private const string BASE_DOMAIN = "hitomi.la";

        [JsonIgnore]
        public long Id { get; set; }
        private string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                IsPlayable = value.EndsWith(".gif");
                FileName = IndexFromNameRegex().Match(value).Groups[1].Value;
                Index = int.Parse(FileName);
            }
        }
        [GeneratedRegex("""(\d+).*""")]
        private static partial Regex IndexFromNameRegex();
        public int Index { get; private set; }
        public string FileName { get; private set; }
        private string _fullFileName;
        public string FullFileName => _fullFileName ??= FileName + '.' + FileExtension;
        public bool IsPlayable { get; private set; }

        private string _imageFilePath;
        public string ImageFilePath {
            get => _imageFilePath ??= Path.Combine(
                Constants.IMAGE_DIR_V3,
                Gallery.Id.ToString(),
                FullFileName
            );
            set => _imageFilePath = value;
        }

        public string Hash { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public int Haswebp { get; set; }
        public int Hasavif { get; set; }
        public int Hasjxl { get; set; }
        private string _fileExtension;
        public string FileExtension {
            get => _fileExtension ??=
                Haswebp == 1 ? "webp" :
                Hasavif == 1 ? "avif" : "jxl";
        }
        
        [Required]
        public Gallery Gallery { get; set; }

        public string GetImageAddress(HashSet<string> subdomainPickerSet, (string notContains, string contains) subdomainCandidates, string serverTime) {
            string hashFragment = Convert.ToInt32(Hash[^1..] + Hash[^3..^1], 16).ToString();
            string subdomain = subdomainPickerSet.Contains(hashFragment) ? subdomainCandidates.contains : subdomainCandidates.notContains;
            return $"https://{subdomain}.{BASE_DOMAIN}/{FileExtension}/{serverTime}/{hashFragment}/{Hash}.{FileExtension}";
        }
    }
}
