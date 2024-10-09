using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class CRUDContentDialogVM : DQObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(CRUDContentDialog).Name);
        public enum CRUDAction {
            Create, Rename, Delete
        }

        private readonly string _oldName;
        private readonly CRUDAction _action;
        private readonly InputValidationVM _inputValidationVM;
        private readonly TFSelectorVM _tfsSelectorVM;
        private readonly ObservableCollection<TagFilter> _tagFilters;

        public string CloseButtonText { get; } = TEXT_CANCEL;
        [ObservableProperty]
        private string _titleText;
        [ObservableProperty]
        private string _primaryButtonText;
        [ObservableProperty]
        private object _content;
        [ObservableProperty]
        private bool _isPrimaryButtonEnabled = false;

        public CRUDContentDialogVM(CRUDAction action, string oldName = null, ObservableCollection<TagFilter> tagFilters = null) {
            if (action == CRUDAction.Rename && oldName == null) {
                throw new ArgumentException($"{nameof(oldName)} must be provided when {nameof(action)} is {nameof(CRUDAction.Rename)}", nameof(action));
            } else if (action == CRUDAction.Delete && tagFilters == null) {
                throw new ArgumentException($"{nameof(tagFilters)} must be provided when {nameof(action)} is {nameof(CRUDAction.Delete)}", nameof(action));
            }
            _action = action;
            _oldName = oldName;
            TitleText = _resourceMap.GetValue($"Title_{_action}").ValueAsString;
            PrimaryButtonText = _resourceMap.GetValue($"Text_{_action}").ValueAsString;
            switch (_action) {
                case CRUDAction.Create or CRUDAction.Rename:
                    _inputValidationVM = new(TagFilter.TAG_FILTER_SET_NAME_MAX_LEN);
                    if (_action == CRUDAction.Rename) {
                        _inputValidationVM.InputText = _oldName;
                        _inputValidationVM.AddValidator(CheckSameNameAsBefore);
                    }
                    _inputValidationVM.AddValidator(CheckDuplicate);
                    _inputValidationVM.PropertyChanged += (object sender, PropertyChangedEventArgs e) => {
                        if (e.PropertyName == nameof(_inputValidationVM.InputText)) {
                            SetIsPrimaryButtonEnabled();
                        }
                    };
                    Content = new InputValidation() { ViewModel = _inputValidationVM };
                    break;
                case CRUDAction.Delete:
                    _tagFilters = tagFilters;
                    _tfsSelectorVM = new(tagFilters);
                    _tfsSelectorVM.SelectedTFCBModels.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                        SetIsPrimaryButtonEnabled();
                    };
                    Content = new TFSSelector() { ViewModel = _tfsSelectorVM };
                    break;
            }
        }

        private void SetIsPrimaryButtonEnabled() {
            IsPrimaryButtonEnabled =
                _action == CRUDAction.Delete ?
                _tfsSelectorVM.SelectedTFCBModels.Any() :
                _inputValidationVM.InputText.Length != 0;
        }

        public void PrimaryButton_Clicked(ContentDialog _0, ContentDialogButtonClickEventArgs args) {
            if (_action != CRUDAction.Delete) {
                args.Cancel = !_inputValidationVM.Validate();
            }
        }

        private bool CheckDuplicate(string name, out string errorMessage) {
            if (_tagFilters.Any(tfs => tfs.Name == name)) {
                errorMessage = string.Format(
                    _resourceMap.GetValue("Error_Message_Duplicate").ValueAsString,
                    name
                );
                return false;
            }
            errorMessage = "";
            return true;
        }

        private bool CheckSameNameAsBefore(string newName, out string errorMessage) {
            if (_oldName == newName) {
                errorMessage = _resourceMap.GetValue("Error_Message_SameNameAsBefore").ValueAsString;
                return false;
            }
            errorMessage = "";
            return true;
        }

        public string GetInputText() {
            return _inputValidationVM.InputText;
        }

        public IEnumerable<TagFilter> GetSelectedTagFilters() {
            return _tfsSelectorVM.GetSelectedTagFilters();
        }
    }
}
