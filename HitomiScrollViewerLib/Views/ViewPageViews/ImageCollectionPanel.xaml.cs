using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace HitomiScrollViewerLib.Views.ViewPageViews {
    public sealed partial class ImageCollectionPanel : UserControl {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ImageCollectionPanelVM),
            typeof(ImageCollectionPanel),
            null
        );

        public ImageCollectionPanelVM ViewModel {
            get => (ImageCollectionPanelVM)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public ImageCollectionPanel() {
            InitializeComponent();
        }

        private static readonly string VIRTUAL_HOST_NAME = "images";
        // {0} = full file name
        // {1} = invert y-axis css
        private static readonly string IMAGE_HTML =
            """<body style="margin: 0;"><img src="http://""" +
            VIRTUAL_HOST_NAME +
            """/{0}" style="width:100%; height:100%;{1}"></img></body>""";
        // CommonSettings.FlowDirection affects WebView2 so gotta invert the images on y-axis
        private const string INVERT_Y_AXIS_STYLE_CSS = "transform: scaleX(-1);";


        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs _1) {
            sender.CoreWebView2.SetVirtualHostNameToFolderMapping(VIRTUAL_HOST_NAME, ViewModel.NonVirtualImageDirPath, CoreWebView2HostResourceAccessKind.DenyCors);
            NavigateToImageHtml(sender);
        }

        private void WebView2_Loaded(object sender, RoutedEventArgs e) {
            var webView2 = sender as WebView2;
            SizeAdjustedImageInfo info = webView2.Tag as SizeAdjustedImageInfo;
            _ = webView2.EnsureCoreWebView2Async();
            if (info.LastFlowDirection != CommonSettings.Main.FlowDirectionModel.Value) {
                NavigateToImageHtml(webView2);
            }
        }

        private static void NavigateToImageHtml(WebView2 webView2) {
            SizeAdjustedImageInfo info = webView2.Tag as SizeAdjustedImageInfo;
            FlowDirection fd = CommonSettings.Main.FlowDirectionModel.Value;
            info.LastFlowDirection = fd;
            webView2.NavigateToString(
                string.Format(
                    IMAGE_HTML,
                    info.FullFileName,
                    fd == FlowDirection.LeftToRight ? "" : INVERT_Y_AXIS_STYLE_CSS
                )
            );
        }

    }
}