using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    internal sealed partial class CRUDActionContentDialog : ContentDialog {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(CRUDActionContentDialog).Name);
        internal enum Action {
            Create, Rename, Delete
        }
        private Action _currAction;
        private string _oldName;

        private readonly InputValidation _inputValidation = new();
        private readonly TagFilterSetSelector _deleteTagFilterSetSelector = new();

        public CRUDActionContentDialog() {
            InitializeComponent();
            DefaultButton = ContentDialogButton.Primary;
            CloseButtonText = TEXT_CANCEL;

            PrimaryButtonClick += (ContentDialog _, ContentDialogButtonClickEventArgs args) => {
                switch (_currAction) {
                    case Action.Create or Action.Rename:
                        args.Cancel = !_inputValidation.Validate();
                        break;
                    case Action.Delete:
                        // should be already handled by IsPrimaryButtonEnabled = _deleteTagFilterSetSelector.AnyChecked;
                        break;
                }
            };
            _deleteTagFilterSetSelector.RegisterPropertyChangedCallback(
                TagFilterSetSelector.AnyCheckedProperty,
                (_, _) => { IsPrimaryButtonEnabled = _deleteTagFilterSetSelector.AnyChecked; }
            );

            Loaded += CRUDActionContentDialog_Loaded;
        }

        private void CRUDActionContentDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Loaded -= CRUDActionContentDialog_Loaded;
            _inputValidation.MaxWidth = MaxWidth;
        }

        internal void Init(ObservableCollection<TagFilterSet> collection) {
            _deleteTagFilterSetSelector.Init(collection);
        }

        private bool CheckDuplicate(string name, TextBlock errorMsgTextBlock) {
            if (TagFilterSetContext.MainContext.TagFilterSets.Any(tagFilterSet => tagFilterSet.Name == name)) {
                errorMsgTextBlock.Text = string.Format(
                    _resourceMap.GetValue("Error_Message_Duplicate").ValueAsString,
                    name
                );
                return false;
            }
            return true;
        }

        private bool CheckSameName(string newName, TextBlock errorMsgTextBlock) {
            if (_oldName == newName) {
                errorMsgTextBlock.Text = _resourceMap.GetValue("Error_Message_SameName").ValueAsString;
                return false;
            }
            return true;
        }

        internal void SetDialogAction(Action action, string oldName = null) {
            _currAction = action;
            switch (action) {
                case Action.Create:
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Create").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Create").ValueAsString;
                    IsPrimaryButtonEnabled = false;
                    _inputValidation.AddValidator(CheckDuplicate);
                    _inputValidation.Text = "";
                    Content = _inputValidation;
                    break;
                case Action.Rename:
                    ArgumentNullException.ThrowIfNull(oldName);
                    _oldName = oldName;
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Rename").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Rename").ValueAsString;
                    IsPrimaryButtonEnabled = true;
                    _inputValidation.AddValidator(CheckDuplicate);
                    _inputValidation.AddValidator(CheckSameName);
                    _inputValidation.Text = oldName;
                    _inputValidation.SelectAllTexts();
                    Content = _inputValidation;
                    break;
                case Action.Delete:
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Delete").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Delete").ValueAsString;
                    IsPrimaryButtonEnabled = _deleteTagFilterSetSelector.AnyChecked;
                    Content = _deleteTagFilterSetSelector;
                    break;
            }
        }

        internal string GetInputText() {
            return _inputValidation.Text;
        }
    }
}
