using CommunityToolkit.WinUI.Controls;
using Hitomi_Scroll_Viewer.Entities;
using Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetSelectorComponent;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using static Hitomi_Scroll_Viewer.Resources;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetEditorComponent {
    public sealed partial class TagFilterSetSelector : DockPanel {
        //private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("TagFilterSetSelector");

        private ObservableCollection<TagFilterCheckBox> _tagFilterCheckBoxes;
        internal TagFilterSetSelector PairTagFilterSelector { get; set; }
        internal readonly ObservableHashSet<int> CheckedIndexes = [];
        internal bool AnyChecked {
            get => (bool)GetValue(AnyCheckedProperty);
            set => SetValue(AnyCheckedProperty, value);
        }

        public static DependencyProperty AnyCheckedProperty { get; } = DependencyProperty.Register(
              nameof(AnyChecked),
              typeof(bool),
              typeof(TagFilterSetSelector),
              new PropertyMetadata(false)
        );

        public bool IsInclude {
            set {
                if (value) {
                    //HeaderTextBlock.Text = _resourceMap.GetValue("HeaderText_Include").ValueAsString; TODO
                    HeaderTextBlock.Text = "HeaderText_Include";
                    HeaderTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                } else {
                    //HeaderTextBlock.Text = _resourceMap.GetValue("HeaderText_Exclude").ValueAsString; TODO
                    HeaderTextBlock.Text = "HeaderText_Exclude";
                    HeaderTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        public TagFilterSetSelector() {
            InitializeComponent();

            CheckedIndexes.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                AnyChecked = CheckedIndexes.Count != 0;
            };
        }

        internal void Init(ObservableCollection<TagFilterSet> collection) {
            collection.CollectionChanged += TagFilterSets_CollectionChanged;
            _tagFilterCheckBoxes?.Clear();
            SearchFilterItemsRepeater.ItemsSource = null;
            _tagFilterCheckBoxes =
                new(
                    collection.Select(
                        (tagFilterSet, i) =>
                            new TagFilterCheckBox(
                                i, tagFilterSet, CheckBox_Checked, CheckBox_Unchecked
                            )
                    )
                );
            SearchFilterItemsRepeater.ItemsSource = _tagFilterCheckBoxes;
        }

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    _tagFilterCheckBoxes.Add(
                        new(
                            _tagFilterCheckBoxes.Count,
                            e.NewItems.Cast<TagFilterSet>().ToList()[0],
                            CheckBox_Checked,
                            CheckBox_Unchecked
                        )
                    );
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _tagFilterCheckBoxes.RemoveAt(e.OldStartingIndex);
                    break;
                // Assuming Replace and Move does not happen
                case NotifyCollectionChangedAction.Replace: break;
                case NotifyCollectionChangedAction.Move: break;
                case NotifyCollectionChangedAction.Reset:
                    _tagFilterCheckBoxes.Clear();
                    _tagFilterCheckBoxes = 
                        new(
                            (sender as ObservableCollection<TagFilterSet>).Select(
                                (tagFilterSet, i) =>
                                    new TagFilterCheckBox(
                                        i, tagFilterSet, CheckBox_Checked, CheckBox_Unchecked
                                    )
                            )
                        );
                    break;
            }
        }

        internal void EnableCheckBox(int i, bool enable) {
            _tagFilterCheckBoxes[i].IsEnabled = enable;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            int i = _tagFilterCheckBoxes.IndexOf(sender as TagFilterCheckBox);
            CheckedIndexes.Add(i);
            PairTagFilterSelector.EnableCheckBox(i, false);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            int i = _tagFilterCheckBoxes.IndexOf(sender as TagFilterCheckBox);
            CheckedIndexes.Remove(i);
            PairTagFilterSelector.EnableCheckBox(i, true);
        }

        internal IEnumerable<TagFilterSet> GetCheckedTagFilterSets() {
            return CheckedIndexes.Select(i => _tagFilterCheckBoxes[i].TagFilterSet);
        }
    }
}
