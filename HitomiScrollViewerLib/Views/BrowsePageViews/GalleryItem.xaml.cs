using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class GalleryItem : UserControl {
        private SolidColorBrush TitleBackgroundBrush { get; set; }
        private SolidColorBrush ArtistBackgroundBrush { get; set; }
        private SolidColorBrush TextForegroundBrush { get; set; }

        private GalleryItemVM _viewModel;
        public GalleryItemVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                string typeBrush = value.Gallery.GalleryType.ToString() + "Brush";
                TitleBackgroundBrush = Resources[typeBrush + Resources["TitleBackgroundBrushNumber"]] as SolidColorBrush;
                ArtistBackgroundBrush = Resources[typeBrush + Resources["ArtistBackgroundBrushNumber"]] as SolidColorBrush;
                TextForegroundBrush = Resources[typeBrush + Resources["TextForegroundBrushNumber"]] as SolidColorBrush;
            }
        }
        public GalleryItem() {
            InitializeComponent();
            // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/windows/sizers/gridsplitter
        }
    }
}
