using BlazorPro.BlazorSize;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class GalleryViewPage : IDisposable {
        [Inject] private IResizeListener ResizeListener { get; set; } = default!;
        [Inject] private IConfiguration AppConfiguration { get; set; } = default!;
        [Inject] private GalleryService GalleryService { get; set; } = default!;
        [Inject] private ViewConfigurationService ViewConfigurationService { get; set; } = default!;
        [Parameter] public int GalleryId { get; set; }

        public Guid Id { get; } = new Guid();

        private ViewGalleryDTO? _gallery;
        private ViewConfigurationDTO _viewConfiguration = new();
        private string _baseImageUrl = "";
        /// <summary>
        /// 0-based image index ranges. Start is inclusive, End is exclusive.
        /// </summary>
        private Range[] _imageIndexRanges = [];
        /// <summary>
        /// 1-based page index
        /// </summary>
        private int _pageIndex = 1;
        private int _pageOffset = 0;
        private BrowserWindowSize _browserWindowSize = new();
        private bool _isAutoScrolling = false;
        private CancellationTokenSource? _autoScrollCts;
        // TODO implment FitMode.Auto calculation
        private FitMode _fitMode = FitMode.Vertical;

        protected override void OnInitialized() {
            _baseImageUrl = AppConfiguration["ApiUrl"] + AppConfiguration["ImageFilePath"] + "?galleryId=" + GalleryId;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                ResizeListener.OnResized += OnResize;
                _viewConfiguration = await ViewConfigurationService.GetConfiguration();
                _gallery ??= await GalleryService.GetViewGalleryDTO(GalleryId);
                CaculateImageIndexGroups();
            }
        }

        private bool CanDecrement() => _pageIndex > 1;
        private bool CanIncrement() => _pageIndex < _imageIndexRanges.Length + 1;
        private void Decrement() {
            _pageIndex--;
            StateHasChanged();
        }
        private void Increment() {
            _pageIndex++;
            StateHasChanged();
        }

        private void OnPageOffsetChanged(int offset) {
            _pageOffset = offset;
            CaculateImageIndexGroups();
        }

        private void OnViewModeChanged(ViewMode mode) {
            _viewConfiguration.ViewMode = mode;
            switch (mode) {
                case ViewMode.Default:
                    break;
                case ViewMode.Scroll:
                    break;
            }
        }

        private void OnImageLayoutModeChange(ImageLayoutMode mode) {
            _viewConfiguration.ImageLayoutMode = mode;
            CaculateImageIndexGroups();
        }

        private void ToggleAutoScroll(bool value) {
            _isAutoScrolling = value;
            if (value) {
                _autoScrollCts = new();
                _ = StartAutoScroll();
            } else {
                _autoScrollCts?.Cancel();
                _autoScrollCts?.Dispose();
            }
        }

        private async Task StartAutoScroll() {
            switch (_viewConfiguration.ViewMode) {
                case ViewMode.Default:
                    while (_isAutoScrolling) {
                        await Task.Delay(_viewConfiguration.AutoPageFlipInterval * 1000, _autoScrollCts!.Token);
                        if (CanIncrement()) {
                            Increment();
                        } else {
                            if (_viewConfiguration.Loop) {
                                _pageIndex = 1;
                            } else {
                                ToggleAutoScroll(false);
                            }
                        }
                        StateHasChanged();
                    }
                    break;
                case ViewMode.Scroll:
                    break;
            }
        }

        private void CaculateImageIndexGroups() {
            if (_gallery == null) {
                return;
            }
            _pageIndex = 1;
            // consider offset, view direction, images per page, image layout mode
            List<Range> indexRanges = [];
            // add offset number of images at first page index range
            if (_pageOffset > 0) {
                indexRanges.Add(new(0, _pageOffset));
            }
            switch (_viewConfiguration.ImageLayoutMode) {
                case ImageLayoutMode.Automatic:
                    double viewportAspectRatio = (double)_browserWindowSize.Width / _browserWindowSize.Height;
                    double remainingAspectRatio = viewportAspectRatio - (double)_gallery.Images.ElementAt(0).Width / _gallery.Images.ElementAt(0).Height;
                    int currStart = _pageOffset;
                    int count = 1;
                    for (int i = _pageOffset + 1; i < _gallery.Images.Count; i++) {
                        double currImgAspectRatio = (double)_gallery.Images.ElementAt(i).Width / _gallery.Images.ElementAt(i).Height;
                        if (currImgAspectRatio > remainingAspectRatio || count == _viewConfiguration.ImagesPerPage) {
                            remainingAspectRatio = viewportAspectRatio - currImgAspectRatio;
                            indexRanges.Add(new(currStart, i));
                            currStart = i;
                            count = 1;
                        } else {
                            count++;
                            remainingAspectRatio -= currImgAspectRatio;
                        }
                    }
                    // add last range
                    indexRanges.Add(new(currStart, _gallery.Images.Count));
                    break;
                case ImageLayoutMode.Fixed:
                    int quotient = (_gallery.Images.Count - _pageOffset) / _viewConfiguration.ImagesPerPage;
                    int remainder = (_gallery.Images.Count - _pageOffset) % _viewConfiguration.ImagesPerPage;
                    IEnumerable<Range> midRanges = Enumerable.Range(0, quotient)
                        .Select(i => new Range(
                            i * _viewConfiguration.ImagesPerPage + _pageOffset + 1,
                            (i + 1) * _viewConfiguration.ImagesPerPage + _pageOffset)
                        );
                    indexRanges.AddRange(midRanges);
                    if (remainder > 0) {
                        indexRanges.Add(new(_gallery.Images.Count - _pageOffset + 1, _gallery.Images.Count));
                    }
                    break;
            }
            _imageIndexRanges = [.. indexRanges];
            StateHasChanged();
        }

        private void OnKeyDown(KeyboardEventArgs e) {
            switch (e.Key) {
                case "ArrowLeft":
                    if (CanDecrement()) Decrement();
                    break;
                case "ArrowRight":
                    if (CanIncrement()) Increment();
                    break;
                case "Space":
                    ToggleAutoScroll(!_isAutoScrolling);
                    break;
            }
        }

        private void OnImageClick(MouseEventArgs e) {
            if (e.Button == 0) {
                if (e.ClientX > _browserWindowSize.Width / 2 && CanIncrement()) {
                    Increment();
                } else if (CanDecrement()) {
                    Decrement();
                }
            }
        }

        public void OnResize(object? sender, BrowserWindowSize size) {
            ToggleAutoScroll(false);
            _browserWindowSize = size;
            CaculateImageIndexGroups();
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            if (_autoScrollCts != null) {
                _autoScrollCts.Cancel();
                _autoScrollCts.Dispose();
            }
            ResizeListener.OnResized -= OnResize;
        }
    }
}
