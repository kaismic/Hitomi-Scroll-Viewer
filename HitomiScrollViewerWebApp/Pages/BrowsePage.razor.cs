using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Components.Dialogs;
using HitomiScrollViewerWebApp.Layout;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using static HitomiScrollViewerData.Entities.Tag;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class BrowsePage : ComponentBase {
        [Inject] private BrowseConfigurationService BrowseConfigurationService { get; set; } = default!;
        [Inject] private GalleryService GalleryService {get;set;} = default!;
        [Inject] private IJSRuntime JsRuntime {get;set;} = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;

        private const string ITEM_HEIGHT = "300px";

        private readonly List<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new List<ChipModel<TagDTO>>())];

        /// <summary>
        /// 1-based page number
        /// </summary>
        private int _pageNum = 1;
        private int _totalPages = 1;
        private BrowseGalleryDTO[] _galleries = [];
        private bool _isLoading = false;
        private ICollection<GallerySortDTO> _activeSorts = [];

        public GalleryLanguageDTO SelectedLanguage {
            get => BrowseConfigurationService.Config.SelectedLanguage;
            set {
                if (BrowseConfigurationService.Config.SelectedLanguage == value) {
                    return;
                }
                BrowseConfigurationService.Config.SelectedLanguage = value;
                _ = Task.Run(async () => {
                    await BrowseConfigurationService.UpdateLanguageAsync(value.Id);
                    await LoadGalleries();
                });
                
            }
        }
        public GalleryTypeDTO SelectedType {
            get => BrowseConfigurationService.Config.SelectedType;
            set {
                if (BrowseConfigurationService.Config.SelectedType == value) {
                    return;
                }
                BrowseConfigurationService.Config.SelectedType = value;
                _ = Task.Run(async () => {
                    await BrowseConfigurationService.UpdateTypeAsync(value.Id);
                    await LoadGalleries();
                });
            }
        }

        public string TitleSearchKeyword {
            get => BrowseConfigurationService.Config.TitleSearchKeyword;
            set {
                if (BrowseConfigurationService.Config.TitleSearchKeyword == value) {
                    return;
                }
                BrowseConfigurationService.Config.TitleSearchKeyword = value;
                _ = Task.Run(async () => {
                    await BrowseConfigurationService.UpdateTitleSearchKeywordAsync(value);
                    await LoadGalleries();
                });
            }
        }

        private int ItemsPerPage {
            get => BrowseConfigurationService.Config.ItemsPerPage;
            set {
                if (BrowseConfigurationService.Config.ItemsPerPage == value) {
                    return;
                }
                BrowseConfigurationService.Config.ItemsPerPage = value;
                _ = Task.Run(async () => {
                    await BrowseConfigurationService.UpdateItemsPerPageAsync(value);
                    await LoadGalleries();
                });
            }
        }

        private bool _isInitialized = false;
        private bool _isRendered = false;

        protected override async Task OnInitializedAsync() {
            if (!BrowseConfigurationService.IsLoaded) {
                await BrowseConfigurationService.Load();
                _activeSorts = [.. BrowseConfigurationService.Config.Sorts.Where(s => s.IsActive)];
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
                for (int i = 0; i < TAG_CATEGORIES.Length; i++) {
                    TagCategory category = TAG_CATEGORIES[i];
                    IEnumerable<TagDTO> tags = BrowseConfigurationService.Config.Tags.Where(t => t.Category == category);
                    foreach (TagDTO tag in tags) {
                        _tagSearchPanelChipModels[i].Add(new ChipModel<TagDTO> { Value = tag });
                    }
                }
                await LoadGalleries();
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
                case AdvancedCollectionChangedAction.RemoveSingle: {
                    int id = e.OldItem!.Value.Id;
                    BrowseConfigurationService.Config.Tags.RemoveAll(t => t.Id == id);
                    _ = BrowseConfigurationService.RemoveTagsAsync([id]);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task OnPageNumChanged(int value) {
            _pageNum = value;
            await LoadGalleries();
        }

        private async Task LoadGalleries() {
            _isLoading = true;
            StateHasChanged();
            BrowseQueryResult result = await GalleryService.GetBrowseQueryResult(_pageNum - 1, BrowseConfigurationService.Config.Id);
            _galleries = [.. result.Galleries];
            //int newTotalPages = (result.TotalGalleryCount / ItemsPerPage) + 1;
            //if (_totalPages != newTotalPages) {
            //    _totalPages = newTotalPages;
            //}
            _totalPages = (result.TotalGalleryCount / ItemsPerPage) + 1; ;
            _isLoading = false;
            StateHasChanged();
        }

        private async Task ShowSortEditDialog() {
            IDialogReference dialogRef = await DialogService.ShowAsync<GallerySortEditDialog>("Sort Galleries");
            DialogResult result = (await dialogRef.Result)!;
            if (result.Canceled) {
                return;
            }
            ICollection<GallerySortDTO> sorts = (ICollection<GallerySortDTO>)result.Data!;
            bool success = await BrowseConfigurationService.UpdateGallerySorts(sorts);
            if (success) {
                BrowseConfigurationService.Config.Sorts = sorts;
                _activeSorts = [.. sorts.Where(s => s.IsActive)];
                Snackbar.Add($"Saved successfully", Severity.Success, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
            } else {
                Snackbar.Add($"Failed to save sorts", Severity.Error, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
            }
            await LoadGalleries();
        }
    }
}
