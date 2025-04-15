using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerWebApp.Models {
    public class DownloadModel {
        public required int GalleryId { get; init; }
        public GalleryMinDTO? Gallery { get; set; }
        public DownloadStatus Status { get; set; } = DownloadStatus.Paused;
        public string StatusMessage { get; set; } = "";
        public int Progress { get; set; }
        public Action? StateHasChanged { get; set; }
    }
}