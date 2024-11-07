using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;
using System.Linq;

namespace HitomiScrollViewerLib.Views.ViewPageViews {
    public sealed partial class GalleryTabViewItem : TabViewItem {
        private GalleryTabViewItemVM _viewModel;
        public GalleryTabViewItemVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel == null) {
                    _viewModel = value;
                    value.ShowActionIconRequested += ShowActionIcon;
                }
            }
        }
        public GalleryTabViewItem() {
            InitializeComponent();

            TopCommandBar.PointerEntered += TopCommandBar_PointerEntered;
            TopCommandBar.Closing += TopCommandBar_Closing;
            foreach (var control in TopCommandBar.PrimaryCommands.Cast<Control>()) {
                control.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        private void TopCommandBar_PointerEntered(object _0, PointerRoutedEventArgs _1) {
            TopCommandBar.Opacity = 1;
            TopCommandBar.IsOpen = true;
        }
        
        private void TopCommandBar_Closing(object _0, object _1) {
            TopCommandBar.Opacity = 0.25;
        }

        private void FlipView_SizeChanged(object _0, SizeChangedEventArgs e) {
            ViewModel.CurrentTabViewSize = e.NewSize;
        }

        private void FlipView_Loaded(object sender, RoutedEventArgs e) {
            FlipView flipView = sender as FlipView;
            flipView.Loaded -= FlipView_Loaded;
            // remove _flipview navigation buttons
            Grid flipViewGrid = VisualTreeHelper.GetChild(flipView, 0) as Grid;
            var children = flipViewGrid.Children;
            for (int i = children.Count - 1; i >= 0; i--) {
                if (children[i] is Button) {
                    children.RemoveAt(i);
                }
            }
        }

        private void ShowActionIcon(string glyph1, string glyph2) {
            if (glyph1 == null) {
                ActionIcon1.Opacity = 0;
            } else {
                ActionIcon1.Opacity = 1;
                ActionIcon1.Glyph = glyph1;
            }
            if (glyph2 == null) {
                ActionIcon2.Opacity = 0;
            } else {
                ActionIcon2.Opacity = 1;
                ActionIcon2.Glyph = glyph2;
            }
            FadeOutStoryboard.Begin();
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

        private void FlipView_PreviewKeyDown(object _0, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Space) {
                ViewModel.IsAutoScrolling = !ViewModel.IsAutoScrolling;
                e.Handled = true;
            }
        }

        private void Root_PreviewKeyDown(object _0, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Space) {
                ViewModel.IsAutoScrolling = !ViewModel.IsAutoScrolling;
                e.Handled = true;
            }
        }
    }
}
