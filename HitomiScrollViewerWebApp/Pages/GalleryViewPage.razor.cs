using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class GalleryViewPage {
        [Inject] IConfiguration AppConfiguration { get; set; } = default!;
        [Inject] private GalleryService GalleryService { get; set; } = default!;
        [Inject] private ViewConfigurationService ViewConfigurationService { get; set; } = default!;
        [Parameter] public int GalleryId { get; set; }
        private GalleryMinDTO? _gallery;
        private ViewConfigurationDTO ViewConfiguration { get; set; } = new();
        private string _baseImageUrl = "";
        /// <summary>
        /// 0-based page index
        /// </summary>
        private int _pageIndex = 0;
        private int _pageOffset = 0;

        // TODO implement ViewMode
        // consider ViewConfiguration properties

        protected override void OnInitialized() {
            _baseImageUrl = AppConfiguration["ApiUrl"] + AppConfiguration["ImageFilePath"] + "?galleryId=" + GalleryId;
            _ = Task.Run(async () => {
                ViewConfiguration = await ViewConfigurationService.GetConfiguration();
                _gallery ??= await GalleryService.GetGalleryMinDTO(GalleryId);
                StateHasChanged();
            });
        }

        private int GetStartIndex() => Math.Max(1, _pageIndex * ViewConfiguration.ImagesPerPage + 1 - _pageOffset);
        private int GetEndIndex() {
            if (_gallery == null) {
                throw new InvalidOperationException("Gallery is not loaded yet.");
            }
            return Math.Min((_pageIndex + 1) * ViewConfiguration.ImagesPerPage - _pageOffset, _gallery.GalleryImagesCount);
        }
        private bool CanDecrement() => _pageIndex > 0;
        private bool CanIncrement() {
            if (_gallery == null) {
                throw new InvalidOperationException("Gallery is not loaded yet.");
            }
            return (_pageIndex + 1) * ViewConfiguration.ImagesPerPage - _pageOffset < _gallery.GalleryImagesCount;
        }
        private void Decrement() {
            _pageIndex--;
            StateHasChanged();
        }
        private void Increment() {
            _pageIndex++;
            StateHasChanged();
        }

        private void OnKeyDown(KeyboardEventArgs e) {
            if (e.Key == "ArrowLeft" && CanDecrement()) {
                Decrement();
            }
            if (e.Key == "ArrowRight" && CanIncrement()) {
                Increment();
            }
        }
    }
}
