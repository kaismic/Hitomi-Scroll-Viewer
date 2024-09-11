using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TFSSelectorVM : ObservableObject {
        private ObservableCollection<TagFilterSet> TagFilterSets {
            set {
                value.CollectionChanged += TagFilterSets_CollectionChanged;
                TfsCheckBoxModels = [];
                foreach (TagFilterSet tfs in value) {
                    TFSCheckBoxModel model = new(
                        tfs,
                        new RelayCommand<TFSCheckBoxModel>(CheckBoxToggleHandler)
                    );
                    TfsCheckBoxModels.Add(model);
                }
                SelectedCBModels = [];
                SelectedCBModels.CollectionChanged += SelectedCheckBoxes_CollectionChanged;
                AnySelected = false;
            }
        }

        [ObservableProperty]
        private ObservableCollection<TFSCheckBoxModel> _tfsCheckBoxModels;

        [ObservableProperty]
        private ObservableConcurrentDictionary<int, TFSCheckBoxModel> _selectedCBModels;

        [ObservableProperty]
        private bool _anySelected;

        public TFSSelectorVM(ObservableCollection<TagFilterSet> tagFilterSets) {
            TagFilterSets = tagFilterSets;
        }

        private void TagFilterSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilterSet>()) {
                        TFSCheckBoxModel model = new(
                            tfs,
                            new RelayCommand<TFSCheckBoxModel>(CheckBoxToggleHandler)
                        );
                        TfsCheckBoxModels.Add(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilterSet>()) {
                        var modelToRemove = TfsCheckBoxModels.FirstOrDefault(model => model.TagFilterSet.Id == tfs.Id);
                        if (modelToRemove != null) {
                            TfsCheckBoxModels.Remove(modelToRemove);
                            SelectedCBModels.Remove(tfs.Id);
                        }
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
            AnySelected = SelectedCBModels.Any();
        }

        public virtual void CheckBoxToggleHandler(TFSCheckBoxModel model) {
            if (model.IsChecked) {
                SelectedCBModels.Add(model.TagFilterSet.Id, model);
            } else {
                SelectedCBModels.Remove(model.TagFilterSet.Id);
            }
        }

        public IEnumerable<TagFilterSet> GetSelectedTFSs() {
            return SelectedCBModels.Values.Select(model => model.TagFilterSet);
        }
    }
}
