using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DAOs;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels {
    public class TFSelectorVM {
        public ObservableCollection<TFCheckBoxModel> TFCheckBoxModels { get; } = [];

        private readonly Dictionary<int, TFCheckBoxModel> _selectedTFCBModels = [];
        public event Action SelectionChanged;

        public TFSelectorVM(TagFilterDAO tagFilterDAO) {
            tagFilterDAO.LocalTagFilters.CollectionChanged += TagFilters_CollectionChanged;
            foreach (TagFilter tfs in tagFilterDAO.LocalTagFilters) {
                TFCheckBoxModel model = new(
                    tfs,
                    new RelayCommand<TFCheckBoxModel>(CheckBox_Toggled)
                );
                TFCheckBoxModels.Add(model);
            }
        }

        private void TagFilters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var tfs in e.NewItems.Cast<TagFilter>()) {
                        TFCheckBoxModel model = new(
                            tfs,
                            new RelayCommand<TFCheckBoxModel>(CheckBox_Toggled)
                        );
                        TFCheckBoxModels.Add(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var tfs in e.OldItems.Cast<TagFilter>()) {
                        var modelToRemove = TFCheckBoxModels.FirstOrDefault(model => model.TagFilter.Id == tfs.Id);
                        if (modelToRemove != null) {
                            TFCheckBoxModels.Remove(modelToRemove);
                            _selectedTFCBModels.Remove(tfs.Id);
                        }
                    }
                    SelectionChanged?.Invoke();
                    break;
                // Assuming these do not happen
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        public virtual void CheckBox_Toggled(TFCheckBoxModel model) {
            if (model.IsChecked) {
                _selectedTFCBModels.Add(model.TagFilter.Id, model);
                SelectionChanged?.Invoke();
            } else {
                _selectedTFCBModels.Remove(model.TagFilter.Id);
                SelectionChanged?.Invoke();
            }
        }

        public IEnumerable<TagFilter> GetSelectedTagFilters() {
            return _selectedTFCBModels.Values.Select(model => model.TagFilter);
        }

        public bool AnySelected() => _selectedTFCBModels.Count > 0;
    }
}
