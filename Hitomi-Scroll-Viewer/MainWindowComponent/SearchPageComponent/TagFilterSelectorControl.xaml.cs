using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using static Hitomi_Scroll_Viewer.Resources;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class TagFilterSelectorControl : DockPanel {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("TagFilterSelector");

        internal ObservableCollection<string> TagFilterNames {
            set {
                value.CollectionChanged += TagFilterNames_CollectionChanged;
                _tagFilterCheckBoxes?.Clear();
                SearchFilterItemsRepeater.ItemsSource = null;
                _tagFilterCheckBoxes = new(value.Select(key => new TagFilterCheckBox(key, CheckBox_Checked, CheckBox_Unchecked)));
                SearchFilterItemsRepeater.ItemsSource = _tagFilterCheckBoxes;
            }
        }
        private ObservableCollection<TagFilterCheckBox> _tagFilterCheckBoxes;
        private readonly TagFilterSelectorControl _pairTagFilterSelector;
        internal bool IsInclude { get; private set; }

        public TagFilterSelectorControl(ObservableCollection<string> tagFilterNames, TagFilterSelectorControl pairTagFilterSelector, bool isInclude) {
            InitializeComponent();
            
            TagFilterNames = tagFilterNames;

            _pairTagFilterSelector = pairTagFilterSelector;

            IsInclude = isInclude;
            if (isInclude) {
                HeaderTextBlock.Text = _resourceMap.GetValue("HeaderText_Include").ValueAsString;
                HeaderTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            } else {
                HeaderTextBlock.Text = _resourceMap.GetValue("HeaderText_Exclude").ValueAsString;
                HeaderTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void TagFilterNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    _tagFilterCheckBoxes.Add(new(e.NewItems[0].ToString(), CheckBox_Checked, CheckBox_Unchecked));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _tagFilterCheckBoxes.RemoveAt(e.OldStartingIndex);
                    break;
                // Assuming Replace and Move does not happen
                case NotifyCollectionChangedAction.Replace: break;
                case NotifyCollectionChangedAction.Move: break;
                case NotifyCollectionChangedAction.Reset:
                    _tagFilterCheckBoxes.Clear();
                    _tagFilterCheckBoxes = new((sender as ObservableCollection<string>).Select(key => new TagFilterCheckBox(key, CheckBox_Checked, CheckBox_Unchecked)));
                    break;
            }
        }

        internal void EnableCheckBox(int idx, bool enable) {
            _tagFilterCheckBoxes[idx].IsEnabled = enable;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            int idx = _tagFilterCheckBoxes.IndexOf(sender as TagFilterCheckBox);
            _pairTagFilterSelector.EnableCheckBox(idx, false);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            int idx = _tagFilterCheckBoxes.IndexOf(sender as TagFilterCheckBox);
            _pairTagFilterSelector.EnableCheckBox(idx, true);
        }

        internal List<int> GetCheckedTagFilterIndexes() {
            List<int> result = [];
            for (int i = 0; i < _tagFilterCheckBoxes.Count; i++) {
                if (_tagFilterCheckBoxes[i].IsChecked == true) {
                    result.Add(i);
                }
            }
            return result;
        }
    }
}
