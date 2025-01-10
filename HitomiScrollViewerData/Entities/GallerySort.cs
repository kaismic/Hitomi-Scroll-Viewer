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
        public GalleryProperty Property { get; init; }
        public SortDirection SortDirection { get; set; }
        public bool IsActive { get; set; }
        public int Index { get; set; }
    }
}
