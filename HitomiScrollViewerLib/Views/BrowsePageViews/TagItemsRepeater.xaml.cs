using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class TagItemsRepeater : Grid {
        public TagItemsRepeaterVM ViewModel { get; set; }
        public TagItemsRepeater() {
            InitializeComponent();
        }
    }
}
