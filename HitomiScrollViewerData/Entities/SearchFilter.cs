using HitomiScrollViewerData.DTOs;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities
{
    public class SearchFilter
    {
        public int Id { get; set; }
        public required string SearchKeywordText { get; set; }
        public required string SearchLink { get; set; }
        public GalleryLanguage Language { get; set; } = default!;
        public GalleryType Type { get; set; } = default!;
        [Required] public SearchConfiguration SearchConfiguration { get; set; } = default!;
        public required ICollection<LabeledTagCollection> LabeledTagCollections { get; set; }

        public SearchFilterDTO ToDTO() => new() {
            Id = Id,
            Language = Language.ToDTO(),
            Type = Type.ToDTO(),
            SearchKeywordText = SearchKeywordText,
            LabeledTagCollections = [.. LabeledTagCollections.Select(ltc => ltc.ToDTO())],
            SearchLink = SearchLink,
            SearchConfigurationId = SearchConfiguration.Id
        };
    }
}
