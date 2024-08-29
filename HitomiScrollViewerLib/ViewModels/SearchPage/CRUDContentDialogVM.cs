using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Views.SearchPage;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Linq;
using System.Windows.Input;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.SearchPage {
    public partial class CRUDContentDialogVM : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(CRUDContentDialog).Name);
        public enum CRUDAction {
            Create, Rename, Delete
        }

        private readonly string _oldName;
        private readonly CRUDAction _action;
        private InputValidationVM _inputValidationVM;
        private TFSSelectorVM _tfsSelectorVM;

        [ObservableProperty]
        private string _titleText;
        [ObservableProperty]
        private string _primaryButtonText;
        public string CloseButtonText { get; } = TEXT_CANCEL;

        [ObservableProperty]
        private object _content;

        public ICommand PrimaryButtonCommand => new RelayCommand(() => { }, CanClickPrimaryButton);

        // TODO test if this works (enables/disables button)
        private bool CanClickPrimaryButton() {
            if (_action == CRUDAction.Delete) {
                return _tfsSelectorVM.AnyChecked;
            }
            return _inputValidationVM.Validate();
        }

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
                    _inputValidationVM = new(Entities.TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN);
                    if (_action == CRUDAction.Rename) {
                        _inputValidationVM.InputText = _oldName;
                        _inputValidationVM.AddValidator(CheckSameNameAsBefore);
                    }
                    _inputValidationVM.AddValidator(CheckDuplicate);
                    Content = new InputValidation() { ViewModel = _inputValidationVM };
                    break;
                case CRUDAction.Delete:
                    _tfsSelectorVM = new() { TagFilterSets = HitomiContext.Main.TagFilterSets.Local.ToObservableCollection() };
                    Content = new TFSSelector() { ViewModel = _tfsSelectorVM };
                    break;
            }
        }

        private bool CheckDuplicate(string name, out string errorMessage) {
            if (HitomiContext.Main.TagFilterSets.Any(tfs => tfs.Name == name)) {
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
    }
}
