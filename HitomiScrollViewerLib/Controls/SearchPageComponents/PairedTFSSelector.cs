using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    internal class PairedTFSSelector: TFSSelector {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(PairedTFSSelector).Name);
        internal PairedTFSSelector OtherPairedSelector { get; set; }

        private readonly TextBlock _headerTextBlock = new() {
            TextWrapping = TextWrapping.WrapWholeWords,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        public string HeaderTextRes { set => _headerTextBlock.Text = _resourceMap.GetValue(value).ValueAsString; }
        public Brush HeaderForeground { set => _headerTextBlock.Foreground = value; }

        public PairedTFSSelector(): base() {
            RowDefinitions.Insert(0, new() { Height = GridLength.Auto });
            _headerTextBlock.FontSize = (double)Application.Current.Resources["SubtitleTextBlockFontSize"];
            SetRow(_headerTextBlock, 0);
            Children.Insert(0, _headerTextBlock);
        }

        internal void EnableCheckBox(int i, bool enable) {
            _tfsCheckBoxes[i].IsEnabled = enable;
        }

        override protected void CheckBox_Checked(object sender, RoutedEventArgs e) {
            base.CheckBox_Checked(sender, e);
            OtherPairedSelector.EnableCheckBox(_tfsCheckBoxes.IndexOf((TFSCheckBox)sender), false);
        }

        override protected void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            base.CheckBox_Unchecked(sender, e);
            OtherPairedSelector.EnableCheckBox(_tfsCheckBoxes.IndexOf((TFSCheckBox)sender), true);
        }
    }
}
