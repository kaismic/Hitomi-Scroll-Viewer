using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public partial class TagFilterSetSelector : Grid {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterSetSelector).Name);

        protected ObservableCollection<TagFilterCheckBox> _tagFilterCheckBoxes;
        protected readonly ObservableHashSet<TagFilterCheckBox> _checkedBoxes = [];
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

        public string HeaderTextRes { set => HeaderTextBlock.Text = _resourceMap.GetValue(value).ValueAsString; }
        public Brush HeaderForeground { set => HeaderTextBlock.Foreground = value; }

        public TagFilterSetSelector() {
            InitializeComponent();

            _checkedBoxes.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                AnyChecked = _checkedBoxes.Count != 0;
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
                                tagFilterSet, CheckBox_Checked, CheckBox_Unchecked
                            )
                    )
                );
            SearchFilterItemsRepeater.ItemsSource = _tagFilterCheckBoxes;
        }

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    _tagFilterCheckBoxes.Add(new(
                        e.NewItems.Cast<TagFilterSet>().ToList()[0],
                        CheckBox_Checked,
                        CheckBox_Unchecked
                    ));
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
                                        tagFilterSet, CheckBox_Checked, CheckBox_Unchecked
                                    )
                            )
                        );
                    break;
            }
        }

        internal IEnumerable<TagFilterSet> GetCheckedTagFilterSets() {
            return _checkedBoxes.Select(box => box.TagFilterSet);
        }

        virtual protected void CheckBox_Checked(object sender, RoutedEventArgs e) {
            _checkedBoxes.Add((TagFilterCheckBox)sender);
        }

        virtual protected void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            _checkedBoxes.Remove((TagFilterCheckBox)sender);
        }
    }
}
