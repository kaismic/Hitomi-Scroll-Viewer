using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class DownloadItemDTO {
        public int Id { get; set; }
        public int GalleryId { get; set; }
        public int DownloadConfigurationId { get; set; }

        public DownloadItem ToEntity() => new() {
            Id = Id,
            GalleryId = GalleryId,
        };
    }
}