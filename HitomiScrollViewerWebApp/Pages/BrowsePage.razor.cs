using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using static HitomiScrollViewerData.Entities.Tag;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class BrowsePage : ComponentBase {
        private readonly ObservableCollection<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new ObservableCollection<ChipModel<TagDTO>>())];

        // TODO create gallery browse result component
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

        public BrowsePage() {
            Initialized += OnInitRenderComplete;
            Rendered += OnInitRenderComplete;
        }

        private bool _isInitialized = false;
        private bool _isRendered = false;
        private event Action? Initialized;
        private event Action? Rendered;

        protected override async Task OnInitializedAsync() {
            if (!PageConfigurationService.IsBrowseConfigurationLoaded) {
                PageConfigurationService.IsBrowseConfigurationLoaded = true;
                PageConfigurationService.BrowseConfiguration = await BrowseService.GetConfigurationAsync();
            }
            _isInitialized = true;
            Initialized?.Invoke();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                await JsRuntime.InvokeVoidAsync("setHeightToSourceHeight", "tag-search-panel-collection", "class", "ltk-search-view", "class");
                _isRendered = true;
                Rendered?.Invoke();
            }
        }

        private void OnInitRenderComplete() {
            if (_isInitialized && _isRendered) {
                for (int i = 0; i < TAG_CATEGORIES.Length; i++) {
                    TagCategory category = TAG_CATEGORIES[i];
                    ObservableCollection<ChipModel<TagDTO>> chipModels = _tagSearchPanelChipModels[i];
                    IEnumerable<TagDTO> tags = PageConfigurationService.BrowseConfiguration.Tags.Where(t => t.Category == category);
                    foreach (TagDTO tag in tags) {
                        chipModels.Add(new ChipModel<TagDTO> { Value = tag });
                    }
                    chipModels.CollectionChanged += ChipModels_CollectionChanged;
                }
                StateHasChanged();
            }
        }

        private void ChipModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            Console.WriteLine("e.Action = " + e.Action);
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    IEnumerable<ChipModel<TagDTO>> addingItems = e.NewItems!.Cast<ChipModel<TagDTO>>();
                    PageConfigurationService.BrowseConfiguration.Tags.AddRange(addingItems.Select(m => m.Value));
                    _ = BrowseService.AddTagsAsync(PageConfigurationService.BrowseConfiguration.Id, addingItems.Select(m => m.Value.Id));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    HashSet<int> removingIds = [.. e.OldItems!.Cast<ChipModel<TagDTO>>().Select(m => m.Value.Id)];
                    PageConfigurationService.BrowseConfiguration.Tags.RemoveAll(t => removingIds.Contains(t.Id));
                    _ = BrowseService.RemoveTagsAsync(PageConfigurationService.BrowseConfiguration.Id, removingIds);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                default:
                    break;
            }
        }
    }
}
