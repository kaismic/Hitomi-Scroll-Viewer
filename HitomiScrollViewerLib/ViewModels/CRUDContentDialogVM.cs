using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
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
        private InputValidationVM _inputValidationVM;
        private TFSelectorVM _tfsSelectorVM;

        public string CloseButtonText { get; } = TEXT_CANCEL;
        [ObservableProperty]
        private string _titleText;
        [ObservableProperty]
        private string _primaryButtonText;
        [ObservableProperty]
        private object _content;
        [ObservableProperty]
        private bool _isPrimaryButtonEnabled = false;

        public CRUDContentDialogVM(CRUDAction action) {
            if (action == CRUDAction.Rename) {
                throw new ArgumentException($"{nameof(CRUDAction)} must be {CRUDAction.Create} or {CRUDAction.Delete}", nameof(action));
            }
            _action = action;
            Init();
        }

        public CRUDContentDialogVM(CRUDAction action, string oldName) {
            if (action != CRUDAction.Rename) {
                throw new ArgumentException($"{nameof(CRUDAction)} must be {CRUDAction.Rename}", nameof(action));
            }
            _action = action;
            _oldName = oldName;
            Init();
        }

        private void Init() {
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
                    _tfsSelectorVM = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());
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
            if (HitomiContext.Main.TagFilters.Any(tfs => tfs.Name == name)) {
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
