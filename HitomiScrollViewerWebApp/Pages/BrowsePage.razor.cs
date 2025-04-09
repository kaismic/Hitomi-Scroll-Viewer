using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static HitomiScrollViewerData.Entities.Tag;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class BrowsePage : ComponentBase {
        private const int MIN_ITEM_HEIGHT = 200; // px
        // if the screen height is more then MIN_ITEM_HEIGHT, screen

        private readonly List<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new List<ChipModel<TagDTO>>())];

        /// <summary>
        /// 1-based page number
        /// </summary>
        private int _page = 1;
        private int _pageCount = 1;
        private GalleryFullDTO[] _galleries = [];

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

        private int ItemsPerPage {
            get => PageConfigurationService.BrowseConfiguration.ItemsPerPage;
            set {
                if (PageConfigurationService.BrowseConfiguration.ItemsPerPage == value) {
                    return;
                }
                PageConfigurationService.BrowseConfiguration.ItemsPerPage = value;
                _ = BrowseService.UpdateItemsPerPageAsync(PageConfigurationService.BrowseConfiguration.Id, value);
            }
        }

        private bool _isInitialized = false;
        private bool _isRendered = false;

        protected override async Task OnInitializedAsync() {
            if (!PageConfigurationService.IsBrowseConfigurationLoaded) {
                PageConfigurationService.IsBrowseConfigurationLoaded = true;
                PageConfigurationService.BrowseConfiguration = await BrowseService.GetConfigurationAsync();
            }
            _isInitialized = true;
            _ = OnInitRenderComplete();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                await JsRuntime.InvokeVoidAsync("setHeightToSourceHeight", "tag-search-panel-collection", "class", "ltk-search-view", "class");
                _isRendered = true;
                _ = OnInitRenderComplete();
            }
        }

        private async Task OnInitRenderComplete() {
            if (_isInitialized && _isRendered) {
                _pageCount = await GalleryService.GetCount();
                for (int i = 0; i < TAG_CATEGORIES.Length; i++) {
                    TagCategory category = TAG_CATEGORIES[i];
                    IEnumerable<TagDTO> tags = PageConfigurationService.BrowseConfiguration.Tags.Where(t => t.Category == category);
                    foreach (TagDTO tag in tags) {
                        _tagSearchPanelChipModels[i].Add(new ChipModel<TagDTO> { Value = tag });
                    }
                }
                StateHasChanged();
            }
        }

        private void OnChipModelsChanged(AdvancedCollectionChangedEventArgs<ChipModel<TagDTO>> e) {
            switch (e.Action) {
                case AdvancedCollectionChangedAction.AddSingle: {
                    PageConfigurationService.BrowseConfiguration.Tags.Add(e.NewItem!.Value);
                    _ = BrowseService.AddTagsAsync(PageConfigurationService.BrowseConfiguration.Id, [e.NewItem!.Value.Id]);
                    break;
                }
                case AdvancedCollectionChangedAction.RemoveMultiple: {
                    HashSet<int> removingIds = [.. e.OldItems!.Select(m => m.Value.Id)];
                    PageConfigurationService.BrowseConfiguration.Tags.RemoveAll(t => removingIds.Contains(t.Id));
                    _ = BrowseService.RemoveTagsAsync(PageConfigurationService.BrowseConfiguration.Id, removingIds);
                    break;
                }
                default:
                    throw new NotImplementedException($"Only {nameof(AdvancedCollectionChangedAction.AddSingle)} and {nameof(AdvancedCollectionChangedAction.RemoveMultiple)} is implemented.");
            }
        }

        private async Task OnPageNumChanged(int value) {
            _page = value;
            _pageCount = await GalleryService.GetCount();
            _galleries = [.. await GalleryService.GetGalleryFullDTOs(_page - 1, ItemsPerPage)];
        }
    }
}
