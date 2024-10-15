using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.DAOs;
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
        private static readonly string SUBTREE_NAME = typeof(CRUDContentDialog).Name;
        public enum CRUDAction {
            Create, Rename, Delete
        }

        private readonly string _oldName;
        private readonly CRUDAction _action;
        private readonly InputValidationVM _inputValidationVM;
        private readonly TFSelectorVM _tfsSelectorVM;

        public string CloseButtonText { get; } = TEXT_CANCEL;
        [ObservableProperty]
        private string _titleText;
        [ObservableProperty]
        private string _primaryButtonText;
        [ObservableProperty]
        private object _content;
        [ObservableProperty]
        private bool _isPrimaryButtonEnabled = false;

        private readonly TagFilterDAO _tagFilterDAO;

        public CRUDContentDialogVM(CRUDAction action, TagFilterDAO tagFilterDAO, string oldName = null) {
            if (action == CRUDAction.Rename && oldName == null) {
                throw new ArgumentException($"{nameof(oldName)} must be provided when {nameof(action)} is {nameof(CRUDAction.Rename)}", nameof(action));
            }
            _action = action;
            _tagFilterDAO = tagFilterDAO;
            _oldName = oldName;
            TitleText = $"Title_{_action}".GetLocalized(SUBTREE_NAME);
            PrimaryButtonText = $"Text_{_action}".GetLocalized(SUBTREE_NAME);
            switch (_action) {
                case CRUDAction.Create or CRUDAction.Rename:
                    _inputValidationVM = new(TagFilter.TAG_FILTER_SET_NAME_MAX_LEN);
                    if (_action == CRUDAction.Rename) {
                        _inputValidationVM.InputText = _oldName;
                        _inputValidationVM.SelectionLength = _oldName.Length;
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
                    _tfsSelectorVM = new(tagFilterDAO);
                    _tfsSelectorVM.SelectionChanged += SetIsPrimaryButtonEnabled;
                    Content = new TFSSelector() { ViewModel = _tfsSelectorVM };
                    break;
            }
        }

        private void SetIsPrimaryButtonEnabled() {
            IsPrimaryButtonEnabled =
                _action == CRUDAction.Delete ?
                _tfsSelectorVM.AnySelected() :
                _inputValidationVM.InputText.Length != 0;
        }

        public void PrimaryButton_Clicked(ContentDialog _0, ContentDialogButtonClickEventArgs args) {
            if (_action != CRUDAction.Delete) {
                args.Cancel = !_inputValidationVM.Validate();
            }
        }

        private bool CheckDuplicate(string name, out string errorMessage) {
            if (_tagFilterDAO.LocalTagFilters.Any(tfs => tfs.Name == name)) {
                errorMessage = string.Format(
                    "Error_Message_Duplicate".GetLocalized(SUBTREE_NAME),
                    name
                );
                return false;
            }
            errorMessage = "";
            return true;
        }

        private bool CheckSameNameAsBefore(string newName, out string errorMessage) {
            if (_oldName == newName) {
                errorMessage = "Error_Message_SameNameAsBefore".GetLocalized(SUBTREE_NAME);
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
