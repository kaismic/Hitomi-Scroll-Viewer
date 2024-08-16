using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public partial class TFSSelector : Grid {
        protected ObservableCollection<TFSCheckBox> _tfsCheckBoxes;
        protected ObservableConcurrentDictionary<long, TFSCheckBox> _checkedBoxes = [];
        internal bool AnyChecked {
            get => (bool)GetValue(AnyCheckedProperty);
            set => SetValue(AnyCheckedProperty, value);
        }

        public static DependencyProperty AnyCheckedProperty { get; } = DependencyProperty.Register(
              nameof(AnyChecked),
              typeof(bool),
              typeof(TFSSelector),
              new PropertyMetadata(false)
        );

        public TFSSelector() {
            InitializeComponent();

            _checkedBoxes.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                AnyChecked = _checkedBoxes.Any();
            };
        }

        public void SetCollectionSource(ObservableCollection<TagFilterSet> collection) {
            collection.CollectionChanged += TagFilterSets_CollectionChanged;
            Refresh(collection);
        }

        private void Refresh(ObservableCollection<TagFilterSet> collection) {
            long[] keys = [.. _checkedBoxes.Keys];
            foreach (var key in keys) {
                _checkedBoxes.Remove(key);
            }
            // gotta remove by iterating because somehow _tfsCheckBoxes?.Clear() doesn't work
            // and causes checkboxes to not properly update
            if (_tfsCheckBoxes != null) {
                int i = _tfsCheckBoxes.Count - 1;
                while (_tfsCheckBoxes.Count > 0) {
                    _tfsCheckBoxes.RemoveAt(i--);
                }
            }
            SearchFilterItemsRepeater.DataContext = null;
            SearchFilterItemsRepeater.ItemsSource = null;
            _tfsCheckBoxes =
                new(collection.Select(
                    tagFilterSet => new TFSCheckBox(tagFilterSet, CheckBox_Checked, CheckBox_Unchecked)
                ));
            SearchFilterItemsRepeater.DataContext = _tfsCheckBoxes;
            SearchFilterItemsRepeater.ItemsSource = _tfsCheckBoxes;
        }

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilterSet>()) {
                        _tfsCheckBoxes.Add(new(tfs, CheckBox_Checked, CheckBox_Unchecked));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var removingTFSs = e.OldItems.Cast<TagFilterSet>();
                    foreach (var tfs in removingTFSs) {
                        _checkedBoxes.Remove(tfs.Id);
                        var removingCheckBox = _tfsCheckBoxes.FirstOrDefault(checkBox => checkBox.TagFilterSet == tfs, null);
                        if (removingCheckBox != null) {
                            _tfsCheckBoxes.Remove(removingCheckBox);
                        }
                    }
                    break;
                // Assuming Replace and Move does not happen
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Refresh(sender as ObservableCollection<TagFilterSet>);
                    break;
            }
        }

        internal IEnumerable<TagFilterSet> GetCheckedTagFilterSets() {
            return _checkedBoxes.Select(pair => pair.Value.TagFilterSet);
        }

        virtual protected void CheckBox_Checked(object sender, RoutedEventArgs e) {
            _checkedBoxes.Add(((TFSCheckBox)sender).TagFilterSet.Id, (TFSCheckBox)sender);
        }

        virtual protected void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            _checkedBoxes.Remove(((TFSCheckBox)sender).TagFilterSet.Id);
        }
    }
}
