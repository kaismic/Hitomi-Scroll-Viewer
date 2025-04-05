using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.Entities
{
    [Index(nameof(GalleryId))]
    public class DownloadItem
    {
        public int Id { get; set; }
        public int GalleryId { get; set; }
        public string Title { get; set; } = "";
        public int Progress { get; set; }
        public int TotalCount { get; set; }

        public DownloadItemDTO ToDTO() => new() {
            Id = Id,
            GalleryId = GalleryId,
            Title = Title,
            Progress = Progress,
            TotalCount = TotalCount
        };
    }
}
