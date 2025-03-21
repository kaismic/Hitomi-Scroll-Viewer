using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerWebApp.Services {
    public class PageConfigurationService {
        public bool IsSearchConfigurationLoaded { get; set; } = false;
        public List<GalleryTypeDTO> Types = [];
        public List<GalleryLanguageDTO> Languages = [];
        public SearchConfigurationDTO SearchConfiguration { get; set; } = new() {
            SearchFilters = [],
            SearchKeywordText = "",
            SelectedExcludeTagFilterIds = [],
            SelectedIncludeTagFilterIds = [],
            SelectedTagFilterId = 0,
            SelectedLanguage = new() { EnglishName = "", Id = 0, IsAll = true, LocalName = "" },
            SelectedType = new() { Id = 0, IsAll = true, Value = "" },
            TagFilters = []
        };
        //public BrowseConfigurationDTO BrowseConfiguration { get; set; }
    }
}
