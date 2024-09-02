using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Controls.SearchPageComponents;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class TFSSelectorVM : ObservableObject {
        private ObservableCollection<TagFilterSet> TagFilterSets {
            set {
                value.CollectionChanged += TagFilterSets_CollectionChanged;
                TfsCheckBoxes = [];
                foreach (TagFilterSet tfs in value) {
                    TFSCheckBox tfsCheckBox = new(tfs);
                    tfsCheckBox.Checked += TFSCheckBox_Checked;
                    tfsCheckBox.Unchecked += TFSCheckBox_Unchecked;
                    TfsCheckBoxes.Add(tfsCheckBox);
                }
                SelectedCheckBoxes = [];
                SelectedCheckBoxes.CollectionChanged += SelectedCheckBoxes_CollectionChanged;
                AnySelected = false;
            }
        }

        [ObservableProperty]
        private ObservableCollection<TFSCheckBox> _tfsCheckBoxes;

        [ObservableProperty]
        private ObservableConcurrentDictionary<int, TFSCheckBox> _selectedCheckBoxes;

        [ObservableProperty]
        private bool _anySelected;

        public TFSSelectorVM(ObservableCollection<TagFilterSet> tagFilterSets) {
            TagFilterSets = tagFilterSets;
        }

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilterSet>()) {
                        TFSCheckBox tfsCheckBox = new(tfs);
                        tfsCheckBox.Checked += TFSCheckBox_Checked;
                        tfsCheckBox.Unchecked += TFSCheckBox_Unchecked;
                        TfsCheckBoxes.Add(tfsCheckBox);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilterSet>()) {
                        TFSCheckBox removingCheckBox = TfsCheckBoxes.FirstOrDefault(tfscb => tfscb.TagFilterSet.Id == tfs.Id);
                        if (removingCheckBox != null) {
                            TfsCheckBoxes.Remove(removingCheckBox);
                        }
                        SelectedCheckBoxes.Remove(tfs.Id);
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

        private void SelectedCheckBoxes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            AnySelected = SelectedCheckBoxes.Any();
        }

        public virtual void TFSCheckBox_Checked(object sender, RoutedEventArgs e) {
            TFSCheckBox tFSCheckBox = sender as TFSCheckBox;
            SelectedCheckBoxes.Add(tFSCheckBox.TagFilterSet.Id, tFSCheckBox);
        }

        public virtual void TFSCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            SelectedCheckBoxes.Remove((sender as TFSCheckBox).TagFilterSet.Id);
        }

        public IEnumerable<TagFilterSet> GetSelectedTFSs() {
            return SelectedCheckBoxes.Values.Select(tfscb => tfscb.TagFilterSet);
        }
    }
}
