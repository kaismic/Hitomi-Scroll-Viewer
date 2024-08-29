using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Controls.SearchPageComponents;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.SearchPage {
    public partial class TFSSelectorVM : ObservableObject {
        [ObservableProperty]
        private ObservableCollection<TagFilterSet> _tagFilterSets;
        partial void OnTagFilterSetsChanged(ObservableCollection<TagFilterSet> value) {
            value.CollectionChanged += TagFilterSets_CollectionChanged;
            TfsCheckBoxes = [];
            foreach (TagFilterSet tfs in value) {
                TFSCheckBox tfsCheckBox = new(tfs);
                tfsCheckBox.Checked += TFSCheckBox_Checked;
                tfsCheckBox.Unchecked += TFSCheckBox_Unchecked;
                TfsCheckBoxes.Add(tfsCheckBox);
            }
            CheckedCheckBoxes = [];
            CheckedCheckBoxes.CollectionChanged += CheckedCheckBoxes_CollectionChanged;
            AnyChecked = false;
        }

        [ObservableProperty]
        private ObservableCollection<TFSCheckBox> _tfsCheckBoxes;

        [ObservableProperty]
        private ObservableConcurrentDictionary<int, TFSCheckBox> _checkedCheckBoxes;

        [ObservableProperty]
        private bool _anyChecked;

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilterSet>()) {
                        TagFilterSets.Add(tfs);
                        TFSCheckBox tfsCheckBox = new(tfs);
                        tfsCheckBox.Checked += TFSCheckBox_Checked;
                        tfsCheckBox.Unchecked += TFSCheckBox_Unchecked;
                        TfsCheckBoxes.Add(tfsCheckBox);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilterSet>()) {
                        TagFilterSets.Remove(tfs);
                        TFSCheckBox removingCheckBox = TfsCheckBoxes.FirstOrDefault(tfscb => tfscb.TagFilterSet.Id == tfs.Id);
                        if (removingCheckBox != null) {
                            TfsCheckBoxes.Remove(removingCheckBox);
                        }
                        CheckedCheckBoxes.Remove(tfs.Id);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    TagFilterSets_CollectionChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                    TagFilterSets_CollectionChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                    break;
                // Assuming Move does not happen
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    TagFilterSets = sender as ObservableCollection<TagFilterSet>;
                    break;
            }
        }

        private void CheckedCheckBoxes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            AnyChecked = CheckedCheckBoxes.Any();
        }

        public virtual void TFSCheckBox_Checked(object sender, RoutedEventArgs e) {
            TFSCheckBox tFSCheckBox = sender as TFSCheckBox;
            CheckedCheckBoxes.Add(tFSCheckBox.TagFilterSet.Id, tFSCheckBox);
        }

        public virtual void TFSCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            CheckedCheckBoxes.Remove((sender as TFSCheckBox).TagFilterSet.Id);
        }

        public IEnumerable<TagFilterSet> GetCheckedTFSs() {
            return CheckedCheckBoxes.Values.Select(tfscb => tfscb.TagFilterSet);
        }
    }
}
