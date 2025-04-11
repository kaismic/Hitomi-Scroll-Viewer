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
        [Inject] private BrowseConfigurationService BrowseConfigurationService { get; set; } = default!;
        [Inject] private GalleryService GalleryService {get;set;} = default!;
        [Inject] private IJSRuntime JsRuntime {get;set;} = default!;

        private const string MIN_ITEM_HEIGHT = "200px";
        // if the screen height is more then MIN_ITEM_HEIGHT, screen

        private readonly List<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new List<ChipModel<TagDTO>>())];

        /// <summary>
        /// 1-based page number
        /// </summary>
        private int _page = 1;
        private int _pageCount = 1;
        private GalleryFullDTO[] _galleries = [];

        public GalleryLanguageDTO SelectedLanguage {
            get => BrowseConfigurationService.Config.SelectedLanguage;
            set {
                if (BrowseConfigurationService.Config.SelectedLanguage == value) {
                    return;
                }
                BrowseConfigurationService.Config.SelectedLanguage = value;
                _ = BrowseConfigurationService.UpdateLanguageAsync(value.Id);
            }
        }
        public GalleryTypeDTO SelectedType {
            get => BrowseConfigurationService.Config.SelectedType;
            set {
                if (BrowseConfigurationService.Config.SelectedType == value) {
                    return;
                }
                BrowseConfigurationService.Config.SelectedType = value;
                _ = BrowseConfigurationService.UpdateTypeAsync(value.Id);
            }
        }

        public string SearchKeywordText {
            get => BrowseConfigurationService.Config.SearchKeywordText;
            set {
                if (BrowseConfigurationService.Config.SearchKeywordText == value) {
                    return;
                }
                BrowseConfigurationService.Config.SearchKeywordText = value;
                _ = BrowseConfigurationService.UpdateSearchKeywordTextAsync(value);
            }
        }

        private int ItemsPerPage {
            get => BrowseConfigurationService.Config.ItemsPerPage;
            set {
                if (BrowseConfigurationService.Config.ItemsPerPage == value) {
                    return;
                }
                BrowseConfigurationService.Config.ItemsPerPage = value;
                _ = BrowseConfigurationService.UpdateItemsPerPageAsync(value);
            }
        }

        private bool _isInitialized = false;
        private bool _isRendered = false;

        protected override async Task OnInitializedAsync() {
            if (!BrowseConfigurationService.IsLoaded) {
                await BrowseConfigurationService.Load();
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
                    IEnumerable<TagDTO> tags = BrowseConfigurationService.Config.Tags.Where(t => t.Category == category);
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
                    BrowseConfigurationService.Config.Tags.Add(e.NewItem!.Value);
                    _ = BrowseConfigurationService.AddTagsAsync([e.NewItem!.Value.Id]);
                    break;
                }
                case AdvancedCollectionChangedAction.RemoveMultiple: {
                    HashSet<int> removingIds = [.. e.OldItems!.Select(m => m.Value.Id)];
                    BrowseConfigurationService.Config.Tags.RemoveAll(t => removingIds.Contains(t.Id));
                    _ = BrowseConfigurationService.RemoveTagsAsync(removingIds);
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
