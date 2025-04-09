using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class BrowseConfigurationDTO
    {
        public int Id { get; set; }
        public required List<TagDTO> Tags { get; set; }
        public required GalleryLanguageDTO SelectedLanguage { get; set; }
        public required GalleryTypeDTO SelectedType { get; set; }
        public required string SearchKeywordText { get; set; }
        public required int ItemsPerPage { get; set; }

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
