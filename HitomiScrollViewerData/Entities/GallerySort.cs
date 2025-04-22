using HitomiScrollViewerData.DTOs;
using MudBlazor;

namespace HitomiScrollViewerData.Entities {
    public enum GalleryProperty {
        Id, Title, UploadTime, LastDownloadTime, Type
    }
    public class GallerySort {
        public int Id { get; private set; }
        public required GalleryProperty Property { get; init; }
        public required SortDirection SortDirection { get; set; }

        public GallerySortDTO ToDTO() => new() {
            Property = Property,
            SortDirection = SortDirection
        };
    }
}
