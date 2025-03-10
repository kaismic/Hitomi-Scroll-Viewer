namespace HitomiScrollViewerData.Entities;
public class BrowseQueryConfiguration {
    public int Id { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
    public required GalleryLanguage GalleryLanguage { get; set; }
    public required GalleryType GalleryType { get; set; }
    public string SearchKeywordText { get; set; } = "";
}