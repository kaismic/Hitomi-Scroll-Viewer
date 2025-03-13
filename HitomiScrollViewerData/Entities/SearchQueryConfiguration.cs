using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities;
public class SearchQueryConfiguration {
    public int Id { get; set; }
    public TagFilter? SelectedTagFilter { get; set; }
    public IEnumerable<int> SelectedIncludeTagFilterIds { get; set; } = [];
    public IEnumerable<int> SelectedExcludeTagFilterIds { get; set; } = [];
    public required GalleryLanguage SelectedLanguage { get; set; }
    public required GalleryType SelectedType { get; set; }
    public string SearchKeywordText { get; set; } = "";

    public SearchQueryConfigurationDTO ToDTO() => new() {
        Id = Id,
        SelectedTagFilter = SelectedTagFilter?.ToTagFilterDTO(),
        SelectedIncludeTagFilterIds = SelectedIncludeTagFilterIds,
        SelectedExcludeTagFilterIds = SelectedExcludeTagFilterIds,
        SelectedLanguage = SelectedLanguage.ToDTO(),
        SelectedType = SelectedType.ToDTO(),
        SearchKeywordText = SearchKeywordText
    };
}