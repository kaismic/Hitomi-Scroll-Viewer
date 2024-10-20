using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace HitomiScrollViewerLib.Views.ViewPageViews {
    public sealed partial class ImageCollectionPanel : UserControl {
        private static readonly string VIRTUAL_HOST_NAME = "images.example";
        // {0} = full file name
        private static readonly string IMAGE_HTML =
            """<body style="margin: 0;"><img src="http://""" +
            VIRTUAL_HOST_NAME +
            """/{0}" style="width:100%; height:100%;"/></body>""";
        public ImageCollectionPanelVM ViewModel { get; set; }

        public ImageCollectionPanel() {
            InitializeComponent();
        }

        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs _1) {
            sender.CoreWebView2.SetVirtualHostNameToFolderMapping(VIRTUAL_HOST_NAME, ViewModel.NonVirtualImageDirPath, CoreWebView2HostResourceAccessKind.DenyCors);
            sender.NavigateToString(string.Format(IMAGE_HTML, (sender.Tag as ImageInfo).FullFileName));
        }
    }
}