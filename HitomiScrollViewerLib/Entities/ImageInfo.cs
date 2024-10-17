using HitomiScrollViewerLib.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Index))]
    public class ImageInfo {
        private const string BASE_DOMAIN = "hitomi.la";

        public long Id { get; set; }
        public int Index { get; set; }
        public string FileName { get; set; }
        private string _fullFileName;
        public string FullFileName => _fullFileName ??= FileName + '.' + FileExtension;
        public bool IsPlayable { get; set; }
        public string ImageFilePath => Path.Combine(Constants.IMAGE_DIR_V3, Gallery.Id.ToString(), FullFileName);

        public string Hash { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string FileExtension { get; set; }
        
        [Required]
        public Gallery Gallery { get; set; }

        public string GetImageAddress(HashSet<string> subdomainPickerSet, (string notContains, string contains) subdomainCandidates, string serverTime) {
            string hashFragment = Convert.ToInt32(Hash[^1..] + Hash[^3..^1], 16).ToString();
            string subdomain = subdomainPickerSet.Contains(hashFragment) ? subdomainCandidates.contains : subdomainCandidates.notContains;
            return $"https://{subdomain}.{BASE_DOMAIN}/{FileExtension}/{serverTime}/{hashFragment}/{Hash}.{FileExtension}";
        }

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
