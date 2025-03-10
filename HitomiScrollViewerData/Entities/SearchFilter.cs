using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities
{
    public class SearchFilter
    {
        public int Id { get; set; }
        public required GalleryLanguage Language { get; init; }
        public required GalleryType Type { get; init; }
        public required string SearchKeywordText { get; init; }
        public required ICollection<LabeledTagCollection> LabeledTagCollections { get; set; }

        public SearchFilterDTO ToDTO() => new() {
            Id = Id,
            Language = Language.ToDTO(),
            Type = Type.ToDTO(),
            SearchKeywordText = SearchKeywordText,
            LabeledTagCollections = [.. LabeledTagCollections.Select(ltc => ltc.ToDTO())]
        };
    }
}
