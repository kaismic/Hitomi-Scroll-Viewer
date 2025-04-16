using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class BrowseConfigurationDTO
    {
        public int Id { get; set; }
        public List<TagDTO> Tags { get; set; } = [];
        public GalleryLanguageDTO SelectedLanguage { get; set; } = new();
        public GalleryTypeDTO SelectedType { get; set; } = new();
        public string SearchKeywordText { get; set; } = "";
        public int ItemsPerPage { get; set; }

        public BrowseConfiguration ToEntity() => new() {
            Id = Id,
            Tags = [.. Tags.Select(t => t.ToEntity())],
            SelectedLanguage = SelectedLanguage.ToEntity(),
            SelectedType = SelectedType.ToEntity(),
            SearchKeywordText = SearchKeywordText,
            ItemsPerPage = ItemsPerPage
        };
    }
}
