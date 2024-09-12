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
    public partial class TFSelectorVM : ObservableObject {
        private ObservableCollection<TagFilter> TagFilters {
            set {
                value.CollectionChanged += TagFilters_CollectionChanged;
                TfsCheckBoxModels = [];
                foreach (TagFilter tfs in value) {
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

        public TFSelectorVM(ObservableCollection<TagFilter> tagFilterSets) {
            TagFilters = tagFilterSets;
        }

        private void TagFilters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilter>()) {
                        TFSCheckBoxModel model = new(
                            tfs,
                            new RelayCommand<TFSCheckBoxModel>(CheckBoxToggleHandler)
                        );
                        TfsCheckBoxModels.Add(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilter>()) {
                        var modelToRemove = TfsCheckBoxModels.FirstOrDefault(model => model.TagFilter.Id == tfs.Id);
                        if (modelToRemove != null) {
                            TfsCheckBoxModels.Remove(modelToRemove);
                            SelectedCBModels.Remove(tfs.Id);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    TagFilters_CollectionChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                    TagFilters_CollectionChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                    break;
                // Assuming Move does not happen
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    TagFilters = sender as ObservableCollection<TagFilter>;
                    break;
            }
        }

        private void SelectedCheckBoxes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            AnySelected = SelectedCBModels.Any();
        }

        public virtual void CheckBoxToggleHandler(TFSCheckBoxModel model) {
            if (model.IsChecked) {
                SelectedCBModels.Add(model.TagFilter.Id, model);
            } else {
                SelectedCBModels.Remove(model.TagFilter.Id);
            }
        }

        public IEnumerable<TagFilter> GetSelectedTagFilters() {
            return SelectedCBModels.Values.Select(model => model.TagFilter);
        }
    }
}
