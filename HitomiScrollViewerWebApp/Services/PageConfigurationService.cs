using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.ViewModels;
namespace HitomiScrollViewerWebApp.Services {
    public class PageConfigurationService {
        public List<GalleryTypeDTO> Types { get; set; } = [];
        public List<GalleryLanguageDTO> Languages { get; set; } = [];
        public bool IsSearchConfigurationLoaded { get; set; } = false;
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
        public bool IsBrowseConfigurationLoaded { get; set; } = false;
        public BrowseConfigurationDTO BrowseConfiguration { get; set; } = new() {
            SelectedLanguage = new() { EnglishName = "", Id = 0, IsAll = true, LocalName = "" },
            SelectedType = new() { Id = 0, IsAll = true, Value = "" },
            SearchKeywordText = "",
            Tags = []
        };
        public bool IsDownloadConfigurationLoaded { get; set; } = false;
        public DownloadConfigurationDTO DownloadConfiguration { get; set; } = new();
        public List<DownloadViewModel> DownloadViewModels { get; set; } = [];
    }
}
