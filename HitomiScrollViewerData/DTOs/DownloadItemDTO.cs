using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class DownloadItemDTO {
        public int Id { get; set; }
        public int GalleryId { get; set; }
        public string Title { get; set; } = "";
        public int Progress { get; set; }
        public int TotalCount { get; set; }

        public DownloadItem ToEntity() => new() {
            Id = Id,
            GalleryId = GalleryId,
            Title = Title,
            Progress = Progress,
            TotalCount = TotalCount
        };
    }
}