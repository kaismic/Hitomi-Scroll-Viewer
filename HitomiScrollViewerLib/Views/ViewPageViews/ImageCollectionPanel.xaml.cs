using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.IO;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.Views.ViewPageViews {
    public sealed partial class ImageCollectionPanel : StackPanel {
        private static readonly string VIRTUAL_HOST_NAME = "images.example";
        // {0} = file name
        // {1} = file extension
        private static readonly string IMAGE_HTML =
            """<body style="margin: 0;"><img src="http://""" +
            VIRTUAL_HOST_NAME +
            """/{0}.{1}" style="width:100%; height:100%;"/></body>""";
        private readonly Dictionary<FrameworkElement, int> _imageIndexDict = [];
        private string _nonVirtualImageDir;

        private ImageCollectionPanelVM _viewModel;
        public ImageCollectionPanelVM ViewModel {
            get => _viewModel;
            set {
                _nonVirtualImageDir = Path.Combine(NON_VIRTUAL_IMAGE_DIR_V3, value.ImageInfos[0].Gallery.Id.ToString());
                _viewModel = value;
                for (int i = 0; i < value.ImageInfos.Length; i++) {
                    FrameworkElement image;
                    if (value.ImageInfos[i].IsPlayable) {
                        WebView2 webView2 = new() {
                            IsHitTestVisible = false
                        };
                        _imageIndexDict.Add(webView2, i);
                        webView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
                        _ = webView2.EnsureCoreWebView2Async();
                        image = webView2;
                    } else {
                        image = new Image() {
                            Source = new BitmapImage() { UriSource = new(value.ImageInfos[i].ImageFilePath) }
                        };
                    }
                    Viewbox wrapper = new() {
                        Child = image
                    };
                    Children.Add(wrapper);
                }
            }
        }

        public ImageCollectionPanel() {
            InitializeComponent();
        }

        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args) {
            sender.CoreWebView2.SetVirtualHostNameToFolderMapping(VIRTUAL_HOST_NAME, _nonVirtualImageDir, CoreWebView2HostResourceAccessKind.DenyCors);
            _imageIndexDict.TryGetValue(sender, out int imageInfoIdx);
            sender.NavigateToString(string.Format(IMAGE_HTML, imageInfoIdx, ViewModel.ImageInfos[imageInfoIdx].FileFormat));
        }
    }
}