using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities
{
    [Index(nameof(GalleryId))]
    public class DownloadItem
    {
        public int Id { get; set; }
        public int GalleryId { get; set; }
        [Required] public DownloadConfiguration DownloadConfiguration { get; set; } = default!;

        public DownloadItemDTO ToDTO() => new() {
            Id = Id,
            GalleryId = GalleryId,
            DownloadConfigurationId = DownloadConfiguration.Id
        };
    }
}
