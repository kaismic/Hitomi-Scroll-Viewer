using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.Views;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TFSelectorVM : DQObservableObject {
        private ObservableCollection<TagFilter> TagFilters {
            set {
                value.CollectionChanged += TagFilters_CollectionChanged;
                TfCheckBoxModels = [];
                foreach (TagFilter tfs in value) {
                    TFCheckBoxModel model = new(
                        tfs,
                        new RelayCommand<TFCheckBoxModel>(CheckBox_Toggled)
                    );
                    TfCheckBoxModels.Add(model);
                }
                SelectedTFCBModels = [];
            }
        }

        [ObservableProperty]
        private ObservableCollection<TFCheckBoxModel> _tfCheckBoxModels;

        [ObservableProperty]
        private ObservableConcurrentDictionary<int, TFCheckBoxModel> _selectedTFCBModels;

        public TFSelectorVM(ObservableCollection<TagFilter> tagFilterSets) {
            TagFilters = tagFilterSets;
        }

        private void TagFilters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilter>()) {
                        TFCheckBoxModel model = new(
                            tfs,
                            new RelayCommand<TFCheckBoxModel>(CheckBox_Toggled)
                        );
                        TfCheckBoxModels.Add(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilter>()) {
                        var modelToRemove = TfCheckBoxModels.FirstOrDefault(model => model.TagFilter.Id == tfs.Id);
                        if (modelToRemove != null) {
                            TfCheckBoxModels.Remove(modelToRemove);
                            SelectedTFCBModels.Remove(tfs.Id);
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

        public virtual void CheckBox_Toggled(TFCheckBoxModel model) {
            if (model.IsChecked) {
                SelectedTFCBModels.Add(model.TagFilter.Id, model);
            } else {
                SelectedTFCBModels.Remove(model.TagFilter.Id);
            }
        }

        public IEnumerable<TagFilter> GetSelectedTagFilters() {
            return SelectedTFCBModels.Values.Select(model => model.TagFilter);
        }
    }
}
