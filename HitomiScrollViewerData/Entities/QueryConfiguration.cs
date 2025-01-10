namespace HitomiScrollViewerData.Entities {
    public enum PageKind {
        SearchPage, BrowsePage
    }
    public class QueryConfiguration {
        public int Id { get; private set; }
        public required PageKind PageKind { get; init; }
        public required GalleryLanguage GalleryLanguage { get; set; }
        public required GalleryType GalleryType { get; set; }
    }
}