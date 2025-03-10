using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class SearchFilterDTO
    {
        public required int Id { get; set; }
        public required GalleryLanguageDTO Language { get; init; }
        public required GalleryTypeDTO Type { get; init; }
        public required string SearchKeywordText { get; init; }
        public required ICollection<LabeledTagCollectionDTO> LabeledTagCollections { get; set; }

        public SearchFilter ToEntity() => new() {
            Id = Id,
            Language = Language.ToEntity(),
            Type = Type.ToEntity(),
            SearchKeywordText = SearchKeywordText,
            LabeledTagCollections = [.. LabeledTagCollections.Select(ltc => ltc.ToEntity())]
        };
    }
}
