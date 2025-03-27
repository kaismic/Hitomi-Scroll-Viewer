using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static HitomiScrollViewerData.Entities.Tag;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class BrowsePage : ComponentBase {
        private readonly List<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new List<ChipModel<TagDTO>>())];

        public GalleryLanguageDTO SelectedLanguage {
            get => PageConfigurationService.BrowseConfiguration.SelectedLanguage;
            set {
                if (PageConfigurationService.BrowseConfiguration.SelectedLanguage == value) {
                    return;
                }
                PageConfigurationService.BrowseConfiguration.SelectedLanguage = value;
                _ = BrowseService.UpdateLanguageAsync(PageConfigurationService.BrowseConfiguration.Id, value.Id);
            }
        }
        public GalleryTypeDTO SelectedType {
            get => PageConfigurationService.BrowseConfiguration.SelectedType;
            set {
                if (PageConfigurationService.BrowseConfiguration.SelectedType == value) {
                    return;
                }
                PageConfigurationService.BrowseConfiguration.SelectedType = value;
                _ = BrowseService.UpdateTypeAsync(PageConfigurationService.BrowseConfiguration.Id, value.Id);
            }
        }

        public string SearchKeywordText {
            get => PageConfigurationService.BrowseConfiguration.SearchKeywordText;
            set {
                if (PageConfigurationService.BrowseConfiguration.SearchKeywordText == value) {
                    return;
                }
                PageConfigurationService.BrowseConfiguration.SearchKeywordText = value;
                _ = BrowseService.UpdateSearchKeywordTextAsync(PageConfigurationService.BrowseConfiguration.Id, value);
            }
        }

        protected override async Task OnInitializedAsync() {
            if (!PageConfigurationService.IsBrowseConfigurationLoaded) {
                PageConfigurationService.IsBrowseConfigurationLoaded = true;
                PageConfigurationService.BrowseConfiguration = await BrowseService.GetConfigurationAsync();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                await JsRuntime.InvokeVoidAsync("setHeightToSourceHeight", "tag-search-panel-collection", "class", "ltk-search-view", "class");
            }
        }

    }
}
