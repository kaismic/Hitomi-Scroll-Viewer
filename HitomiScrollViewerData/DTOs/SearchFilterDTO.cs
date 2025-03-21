using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class SearchFilterDTO
    {
        public int Id { get; set; }
        public required string SearchKeywordText { get; init; }
        public required string SearchLink { get; set; }
        public required GalleryLanguageDTO Language { get; init; }
        public required GalleryTypeDTO Type { get; init; }
        public required ICollection<LabeledTagCollectionDTO> LabeledTagCollections { get; set; }
        public int SearchConfigurationId { get; set; }

        public SearchFilter ToEntity() => new() {
            Id = Id,
            SearchKeywordText = SearchKeywordText,
            SearchLink = SearchLink,
            LabeledTagCollections = [.. LabeledTagCollections.Select(ltc => ltc.ToEntity())]
        };
    }
}
