using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities;
public class SearchQueryConfiguration {
    public int Id { get; set; }
    public TagFilter? SelectedTagFilter { get; set; }
    public IEnumerable<int> SelectedIncludeTagFilterIds { get; set; } = [];
    public IEnumerable<int> SelectedExcludeTagFilterIds { get; set; } = [];
    public required GalleryLanguage GalleryLanguage { get; set; }
    public required GalleryType GalleryType { get; set; }
    public string SearchKeywordText { get; set; } = "";

    public SearchQueryConfigurationDTO ToDTO() => new() {
        Id = Id,
        SelectedTagFilter = SelectedTagFilter?.ToTagFilterDTO(),
        SelectedIncludeTagFilterIds = SelectedIncludeTagFilterIds,
        SelectedExcludeTagFilterIds = SelectedExcludeTagFilterIds,
        GalleryLanguage = GalleryLanguage.ToDTO(),
        GalleryType = GalleryType.ToDTO(),
        SearchKeywordText = SearchKeywordText
    };
}