using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class BrowseConfigurationDTO
    {
        public required int Id { get; set; }
        public required ICollection<TagDTO> Tags { get; set; }
        public required GalleryLanguageDTO SelectedLanguage { get; set; }
        public required GalleryTypeDTO SelectedType { get; set; }
        public required string SearchKeywordText { get; set; }

        public BrowseConfiguration ToEntity() => new() {
            Id = Id,
            Tags = [.. Tags.Select(t => t.ToEntity())],
            SelectedLanguage = SelectedLanguage.ToEntity(),
            SelectedType = SelectedType.ToEntity(),
            SearchKeywordText = SearchKeywordText
        };
    }
}
