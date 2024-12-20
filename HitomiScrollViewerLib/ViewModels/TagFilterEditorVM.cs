﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.DAOs;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Xaml.Controls;
using Soluling;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TagFilterEditorVM : DQObservableObject {
        private static readonly string SUBTREE_NAME = typeof(TagFilterEditor).Name;

        [ObservableProperty]
        private bool _isTFAutoSaveEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] ??= true);
        partial void OnIsTFAutoSaveEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] = value;
        }

        public event Action<TagFilter> SelectedTagFilterChanged;
        public event Action<RequestTagsEventArgs> CurrentTagsRequested;

        public class RequestTagsEventArgs : EventArgs {
            private ICollection<Tag> _tags;
            public ICollection<Tag> Tags {
                get {
                    ArgumentNullException.ThrowIfNull(_tags);
                    return _tags;
                }
                set => _tags = value;
            }
        }

        [ObservableProperty]
        private TagFilter _selectedTagFilter;
        partial void OnSelectedTagFilterChanged(TagFilter oldValue, TagFilter newValue) {
            SaveButtonCommand.NotifyCanExecuteChanged();
            RenameButtonCommand.NotifyCanExecuteChanged();
            if (oldValue is not null && IsTFAutoSaveEnabled) {
                // do not save if this selection change occurred due to deletion of currently selected tag filter
                if (DeletedTagFilterIds == null) {
                    SaveTagFilter(oldValue);
                } else if (!DeletedTagFilterIds.Contains(oldValue.Id)) {
                    SaveTagFilter(oldValue);
                }
            }
            if (newValue is not null) {
                SelectedTagFilterChanged?.Invoke(newValue);
            }
        }

        public TagFilterDAO TagFilterDAO { get; }

        public TagFilterEditorVM(TagFilterDAO tagFilterDAO) {
            TagFilterDAO = tagFilterDAO;
            CreateButtonCommand = new RelayCommand(CreateButton_Click);
            RenameButtonCommand = new RelayCommand(
                RenameButton_Click,
                () => SelectedTagFilter != null
            );
            SaveButtonCommand = new RelayCommand(
                () => SaveTagFilter(SelectedTagFilter),
                () => SelectedTagFilter != null
            );
            DeleteButtonCommand = new RelayCommand(DeleteButton_Click);
        }

        private HashSet<int> DeletedTagFilterIds { get; set; }
        public event Func<CRUDContentDialogVM, IAsyncOperation<ContentDialogResult>> ShowCRUDContentDialogRequested;

        public RelayCommand CreateButtonCommand { get; }

        private async void CreateButton_Click() {
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Create, TagFilterDAO);
            if (await ShowCRUDContentDialogRequested?.Invoke(cdvm) != ContentDialogResult.Primary) {
                return;
            }
            string name = cdvm.GetInputText();
            RequestTagsEventArgs args = new();
            CurrentTagsRequested?.Invoke(args);
            TagFilter tf = new() {
                Name = name,
                Tags = args.Tags
            };
            TagFilterDAO.Add(tf);
            SelectedTagFilter = tf;
            MainWindowVM.ShowPopup(
                string.Format(
                    "InfoBar_Message_Create_Complete".GetLocalized(SUBTREE_NAME),
                    name
                )
            );
        }

        public RelayCommand RenameButtonCommand { get; }

        private async void RenameButton_Click() {
            string oldName = SelectedTagFilter.Name;
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Rename, TagFilterDAO, oldName);
            if (await ShowCRUDContentDialogRequested?.Invoke(cdvm) != ContentDialogResult.Primary) {
                return;
            }
            string newName = cdvm.GetInputText();
            TagFilterDAO.UpdateName(SelectedTagFilter, newName);
            MainWindowVM.ShowPopup(
                string.Format(
                    "InfoBar_Message_Rename_Complete".GetLocalized(SUBTREE_NAME),
                    oldName,
                    newName
                )
            );
        }


        public RelayCommand SaveButtonCommand { get; }

        public void SaveTagFilter(TagFilter tf) {
            if (tf == null) {
                return;
            }
            RequestTagsEventArgs args = new();
            CurrentTagsRequested.Invoke(args);
            TagFilterDAO.UpdateTags(tf, args.Tags);
            MainWindowVM.ShowPopup(
                string.Format(
                    "InfoBar_Message_Save_Complete".GetLocalized(SUBTREE_NAME),
                    tf.Name
                )
            );
        }

        public RelayCommand DeleteButtonCommand { get; }

        private async void DeleteButton_Click() {
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Delete, TagFilterDAO);
            if (await ShowCRUDContentDialogRequested?.Invoke(cdvm) != ContentDialogResult.Primary) {
                return;
            }
            IEnumerable<TagFilter> SelectedTagFilters = cdvm.GetSelectedTagFilters();
            DeletedTagFilterIds = SelectedTagFilters.Select(tf => tf.Id).ToHashSet();
            TagFilterDAO.RemoveRange(cdvm.GetSelectedTagFilters());
            MainWindowVM.ShowPopup(
                MultiPattern.Format(
                    "InfoBar_Message_Delete_Complete".GetLocalized(SUBTREE_NAME),
                    DeletedTagFilterIds.Count
                )
            );
        }
    }
}
