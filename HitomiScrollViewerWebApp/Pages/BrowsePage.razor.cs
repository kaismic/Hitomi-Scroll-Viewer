﻿using HitomiScrollViewerData;
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

        private readonly List<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new List<ChipModel<TagDTO>>())];

        /// <summary>
        /// 1-based page number
        /// </summary>
        private int _pageNum = 1;
        private int _totalPages = 1;
        private BrowseGalleryDTO[] _galleries = [];
        private bool[] _selections = [];
        private bool _isLoading = false;
        private bool _isEditing = false;
        private ICollection<GallerySortDTO> _activeSorts = [];

        private async Task OnSelectedLanguageChanged(GalleryLanguageDTO value) {
            BrowseConfigurationService.Config.SelectedLanguage = value;
            await BrowseConfigurationService.UpdateLanguageAsync(value.Id);
            if (BrowseConfigurationService.Config.AutoRefresh) {
                await LoadGalleries();
            }
        }
        
        private async Task OnSelectedTypeChanged(GalleryTypeDTO value) {
            BrowseConfigurationService.Config.SelectedType = value;
            await BrowseConfigurationService.UpdateTypeAsync(value.Id);
            if (BrowseConfigurationService.Config.AutoRefresh) {
                await LoadGalleries();
            }
        }
        
        private async Task OnTitleSearchKeywordChanged(string value) {
            BrowseConfigurationService.Config.TitleSearchKeyword = value;
            await BrowseConfigurationService.UpdateTitleSearchKeywordAsync(value);
            if (BrowseConfigurationService.Config.AutoRefresh) {
                await LoadGalleries();
            }
        }

        private async Task OnItemsPerPageChanged(int value) {
            BrowseConfigurationService.Config.ItemsPerPage = value;
            await BrowseConfigurationService.UpdateItemsPerPageAsync(value);
            if (BrowseConfigurationService.Config.AutoRefresh) {
                await LoadGalleries();
            }
        }

        private async Task OnAutoRefreshChanged(bool value) {
            BrowseConfigurationService.Config.AutoRefresh = value;
            await BrowseConfigurationService.UpdateAutoRefreshAsync(value);
            if (BrowseConfigurationService.Config.AutoRefresh) {
                await LoadGalleries();
            }
        }

        private bool _isInitialized = false;
        private bool _isRendered = false;

        protected override async Task OnInitializedAsync() {
            await BrowseConfigurationService.Load();
            _activeSorts = [.. BrowseConfigurationService.Config.Sorts.Where(s => s.IsActive)];
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
            _ = Task.Run(async () => {
                switch (e.Action) {
                    case AdvancedCollectionChangedAction.AddSingle: {
                        BrowseConfigurationService.Config.Tags.Add(e.NewItem!.Value);
                        await BrowseConfigurationService.AddTagsAsync([e.NewItem!.Value.Id]);
                        break;
                    }
                    case AdvancedCollectionChangedAction.RemoveMultiple: {
                        HashSet<int> removingIds = [.. e.OldItems!.Select(m => m.Value.Id)];
                        BrowseConfigurationService.Config.Tags.RemoveAll(t => removingIds.Contains(t.Id));
                        await BrowseConfigurationService.RemoveTagsAsync(removingIds);
                        break;
                    }
                    case AdvancedCollectionChangedAction.RemoveSingle: {
                        int id = e.OldItem!.Value.Id;
                        BrowseConfigurationService.Config.Tags.RemoveAll(t => t.Id == id);
                        await BrowseConfigurationService.RemoveTagsAsync([id]);
                        break;
                    }
                    default:
                        throw new NotImplementedException();
                }
                 if (BrowseConfigurationService.Config.AutoRefresh) {
                     _ = LoadGalleries();
                 }
            });
        }

        private async Task OnPageNumChanged(int value) {
            _pageNum = value;
            await LoadGalleries();
        }

        private async Task LoadGalleries() {
            _isLoading = true;
            StateHasChanged();
            BrowseQueryResult result = await GalleryService.GetBrowseQueryResult(_pageNum - 1, BrowseConfigurationService.Config.Id);
            BrowseGalleryDTO[] temp = [.. result.Galleries];
            _selections = new bool[temp.Length];
            _galleries = temp;
            _totalPages = (result.TotalGalleryCount / BrowseConfigurationService.Config.ItemsPerPage) + 1; ;
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
                Snackbar.Add("Saved successfully", Severity.Success, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
                await LoadGalleries();
            } else {
                Snackbar.Add("Save failed", Severity.Error, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
            }
        }

        private async Task DeleteGalleries() {
            List<int> ids = [];
            for (int i = 0; i < _selections.Length; i++) {
                if (_selections[i]) {
                    ids.Add(_galleries[i].Id);
                }
            }
            bool success = await GalleryService.DeleteGalleries(ids);
            if (success) {
                Snackbar.Add($"Deleted {ids.Count} galleries.", Severity.Success, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
                await LoadGalleries();
            } else {
                Snackbar.Add("Deletion failed.", Severity.Error, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
            }
        }

        private async Task DeleteGallery(int id) {
            bool success = await GalleryService.DeleteGalleries([id]);
            if (success) {
                Snackbar.Add($"Deletion success.", Severity.Success, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
                await LoadGalleries();
            } else {
                Snackbar.Add("Deletion failed.", Severity.Error, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
            }
        }
    }
}