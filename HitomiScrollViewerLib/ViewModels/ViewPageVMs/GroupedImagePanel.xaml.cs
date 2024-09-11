using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using static HitomiScrollViewerLib.Controls.ViewPage;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.Controls.ViewPageComponents {
    public sealed partial class GroupedImagePanel : DockPanel {
        private static readonly string VIRTUAL_HOST_NAME = "images.example";
        // {0} = file name
        // {1} = file extension
        private static readonly string imageHtml =
            """<body style="margin: 0;"><img src="http://""" +
            VIRTUAL_HOST_NAME +
            """/{0}.{1}" style="width:100%; height:100%;"/></body>""";

        private readonly List<FrameworkElement> _imageContainers = [];
        private readonly List<ImageInfo> _imageInfos = [];
        private readonly string _imageDir;
        private readonly string _nonVirtualImageDir;
        private readonly Dictionary<FrameworkElement, int> imageContainerIndexDict = [];

        public GroupedImagePanel(ViewDirection viewDirection, Range range, Gallery gallery) {
            InitializeComponent();
            _imageDir = Path.Combine(IMAGE_DIR_V2, gallery.Id.ToString());
            _nonVirtualImageDir = Path.Combine(NON_VIRTUAL_IMAGE_DIR_V2, gallery.Id.ToString());
            for (int i = range.Start.Value; i < range.End.Value; i++) {
                string[] files = Directory.GetFiles(_imageDir, i.ToString() + ".*");
                FrameworkElement imageContainer;
                // playable image
                if (gallery.Files.First(imageInfo => imageInfo.Index == i).Name.Contains(".gif")) {
                    WebView2 webView2 = new() {
                        IsHitTestVisible = false
                    };
                    imageContainerIndexDict.Add(webView2, i);
                    webView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
                    DispatcherQueue.TryEnqueue(
                    async () => {
                            await webView2.EnsureCoreWebView2Async();
                        }
                    );
                    imageContainer = webView2;
                }
                // non-playable image
                else {
                    Image image = new() {
                        Source = files.Length > 0 ? new BitmapImage(new(files[0])) : null
                    };
                    imageContainerIndexDict.Add(image, i);
                    imageContainer = image;
                }
                _imageContainers.Add(imageContainer);
                _imageInfos.Add(gallery.Files.First(imageInfo => imageInfo.Index == i));
            }
            foreach (var image in _imageContainers) {
                Children.Add(image);
            }
            UpdateViewDirection(viewDirection);
        }

        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args) {
            sender.CoreWebView2.SetVirtualHostNameToFolderMapping(VIRTUAL_HOST_NAME, _nonVirtualImageDir, CoreWebView2HostResourceAccessKind.DenyCors);
            TrySetImageSource(sender);
        }

        public void UpdateViewDirection(ViewDirection viewDirection) {
            foreach (var image in _imageContainers) {
                SetDock(image, viewDirection == ViewDirection.LeftToRight ? Dock.Left : Dock.Right);
            }
        }

        public void SetImageSizes(Size viewportSize) {
            double maxImgWidth = viewportSize.Width / _imageContainers.Count;
            double maxImgHeight = viewportSize.Height;
            double idealAspectRatio = maxImgWidth / maxImgHeight;
            for (int i = 0; i < _imageContainers.Count; i++) {
                double imgAspectRatio = (double)_imageInfos[i].Width / _imageInfos[i].Height;
                _imageContainers[i].Width = imgAspectRatio >= idealAspectRatio ? maxImgWidth : maxImgHeight * imgAspectRatio;
                _imageContainers[i].Height = imgAspectRatio < idealAspectRatio ? maxImgHeight : maxImgWidth / imgAspectRatio;
                if (_imageContainers[i] is Image img && img.Source != null) {
                    (img.Source as BitmapImage).DecodePixelWidth = (int)img.Width;
                }
            }
        }

        public void ResetImageSizes() {
            foreach (var imageContainer in _imageContainers) {
                imageContainer.Width = double.NaN;
                imageContainer.Height = double.NaN;
                if (imageContainer is Image img && img.Source != null) {
                    (img.Source as BitmapImage).DecodePixelWidth = 0;
                    (img.Source as BitmapImage).DecodePixelHeight = 0;
                }
            }
        }

        public void RefreshImages() {
            _imageContainers.ForEach(TrySetImageSource);
        }

        private void TrySetImageSource(FrameworkElement imageContainer) {
            imageContainerIndexDict.TryGetValue(imageContainer, out int idx);
            string[] files = Directory.GetFiles(_imageDir, idx.ToString() + ".*");
            if (files.Length > 0) {
                if (imageContainer is Image img && img.Source == null) {
                    img.Source = new BitmapImage(new(files[0]));
                } else if (imageContainer is WebView2 webView2) {
                    webView2.NavigateToString(string.Format(imageHtml, idx, files[0].Split('.')[^1]));
                }
            }
        }
    }
}
