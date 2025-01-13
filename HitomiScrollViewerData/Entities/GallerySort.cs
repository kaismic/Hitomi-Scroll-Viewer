using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.Entities {
    public enum GalleryProperty {
        Id, Title, Date, LastDownloadTime, Type, Language
    }
    public enum SortDirection {
        Ascending, Descending
    }
    [Index(nameof(IsActive))]
    public class GallerySort {
        public int Id { get; private set; }
        public required GalleryProperty Property { get; init; }
        public required SortDirection SortDirection { get; set; }
        public required bool IsActive { get; set; }
        public int Index { get; set; }
    }
}
