namespace HitomiScrollViewerData.DTOs
{
    public class SearchQueryConfigurationDTO
    {
        public int Id { get; set; }
        public required TagFilterDTO? SelectedTagFilter { get; set; }
        public required IEnumerable<int> SelectedIncludeTagFilterIds { get; set; }
        public required IEnumerable<int> SelectedExcludeTagFilterIds { get; set; }
        public required GalleryLanguageDTO SelectedLanguage { get; set; }
        public required GalleryTypeDTO SelectedType { get; set; }
        public required string SearchKeywordText { get; set; }
    }
}