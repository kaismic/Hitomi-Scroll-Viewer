using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public partial class TFSSelector : Grid {
        protected ObservableCollection<TFSCheckBox> _tfsCheckBoxes;
        protected ObservableConcurrentDictionary<TagFilterSet, TFSCheckBox> _checkedBoxes = [];
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

        internal void Init(ObservableCollection<TagFilterSet> collection) {
            collection.CollectionChanged += TagFilterSets_CollectionChanged;
            _tfsCheckBoxes?.Clear();
            SearchFilterItemsRepeater.ItemsSource = null;
            _tfsCheckBoxes =
                new(
                    collection.Select(
                        (tagFilterSet, i) =>
                            new TFSCheckBox(
                                tagFilterSet, CheckBox_Checked, CheckBox_Unchecked
                            )
                    )
                );
            SearchFilterItemsRepeater.ItemsSource = _tfsCheckBoxes;
        }

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    Trace.WriteLine("Adding:");
                    foreach (var tfs in e.NewItems.Cast<TagFilterSet>()) {
                        Trace.WriteLine(tfs.Name);
                        _tfsCheckBoxes.Add(new(tfs, CheckBox_Checked, CheckBox_Unchecked));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var removingTFSs = e.OldItems.Cast<TagFilterSet>();
                    foreach (var tfs in removingTFSs) {
                        _checkedBoxes.Remove(tfs);
                    }
                    Trace.WriteLine("Removing:");
                    foreach (var tfs in removingTFSs) {
                        var removingCheckBox = _tfsCheckBoxes.FirstOrDefault(checkBox => checkBox.TagFilterSet == tfs, null);
                        if (removingCheckBox != null) {
                            Trace.WriteLine(removingCheckBox.TagFilterSet.Name);
                            _tfsCheckBoxes.Remove(removingCheckBox);
                        }

                    }
                    break;
                // Assuming Replace and Move does not happen
                case NotifyCollectionChangedAction.Replace: break;
                case NotifyCollectionChangedAction.Move: break;
                case NotifyCollectionChangedAction.Reset:
                    _tfsCheckBoxes.Clear();
                    _tfsCheckBoxes =
                        new(
                            (sender as ObservableCollection<TagFilterSet>).Select(
                                (tagFilterSet, i) =>
                                    new TFSCheckBox(tagFilterSet, CheckBox_Checked, CheckBox_Unchecked)
                            )
                        );
                    foreach (var pair in _checkedBoxes) {
                        _checkedBoxes.Remove(pair.Key);
                    }
                    break;
            }
        }

        internal IEnumerable<TagFilterSet> GetCheckedTagFilterSets() {
            return _checkedBoxes.Select(pair => pair.Value.TagFilterSet);
        }

        virtual protected void CheckBox_Checked(object sender, RoutedEventArgs e) {
            _checkedBoxes.Add(((TFSCheckBox)sender).TagFilterSet, (TFSCheckBox)sender);
        }

        virtual protected void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            _checkedBoxes.Remove(((TFSCheckBox)sender).TagFilterSet);
        }
    }
}
