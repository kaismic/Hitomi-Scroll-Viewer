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
    public partial class TFSelectorVM : ObservableObject {
        private ObservableCollection<TagFilter> TagFilters {
            set {
                value.CollectionChanged += TagFilters_CollectionChanged;
                TFCheckBoxModels = [];
                foreach (TagFilter tfs in value) {
                    TFCheckBoxModel model = new(
                        tfs,
                        new RelayCommand<TFCheckBoxModel>(CheckBoxToggleHandler)
                    );
                    TFCheckBoxModels.Add(model);
                }
                SelectedTFCBModels = [];
                SelectedTFCBModels.CollectionChanged += SelectedCheckBoxes_CollectionChanged;
                AnySelected = false;
            }
        }

        private ObservableCollection<TFCheckBoxModel> _tfCheckBoxModels;
        public ObservableCollection<TFCheckBoxModel> TFCheckBoxModels {
            get => _tfCheckBoxModels;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _tfCheckBoxModels, value);
                });
            }
        }
        private ObservableConcurrentDictionary<int, TFCheckBoxModel> _selectedTFCBModels;
        public ObservableConcurrentDictionary<int, TFCheckBoxModel> SelectedTFCBModels {
            get => _selectedTFCBModels;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _selectedTFCBModels, value);
                });
            }
        }

        public bool AnySelected { get; private set; } = false;

        public TFSelectorVM(ObservableCollection<TagFilter> tagFilterSets) {
            TagFilters = tagFilterSets;
        }

        private void TagFilters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilter>()) {
                        TFCheckBoxModel model = new(
                            tfs,
                            new RelayCommand<TFCheckBoxModel>(CheckBoxToggleHandler)
                        );
                        TFCheckBoxModels.Add(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilter>()) {
                        var modelToRemove = TFCheckBoxModels.FirstOrDefault(model => model.TagFilter.Id == tfs.Id);
                        if (modelToRemove != null) {
                            TFCheckBoxModels.Remove(modelToRemove);
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

        private void SelectedCheckBoxes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            AnySelected = SelectedTFCBModels.Any();
        }

        public virtual void CheckBoxToggleHandler(TFCheckBoxModel model) {
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
