using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities;
public class BrowseConfiguration {
    public int Id { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
    public required GalleryLanguage SelectedLanguage { get; set; }
    public required GalleryType SelectedType { get; set; }
    public string SearchKeywordText { get; set; } = "";
    public required int ItemsPerPage { get; set; }

    public BrowseConfigurationDTO ToDTO() => new() {
        Id = Id,
        Tags = [.. Tags.Select(t => t.ToDTO())],
        SelectedLanguage = SelectedLanguage.ToDTO(),
        SelectedType = SelectedType.ToDTO(),
        SearchKeywordText = SearchKeywordText,
        ItemsPerPage = ItemsPerPage
    };
}