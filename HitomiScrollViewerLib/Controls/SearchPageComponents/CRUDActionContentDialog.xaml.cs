using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
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
        private readonly TFSSelector _deleteTFSSelector = new();
        private bool _isInAction = false;
        internal int DeletionCount { get; set; }

        public CRUDActionContentDialog() {
            InitializeComponent();
            DefaultButton = ContentDialogButton.Primary;
            CloseButtonText = TEXT_CANCEL;

            PrimaryButtonClick += (ContentDialog _, ContentDialogButtonClickEventArgs args) => {
                _inputValidation.ClearErrorMsg();
                if (_currAction == Action.Create || _currAction == Action.Rename) {
                    if (!_inputValidation.Validate()) {
                        args.Cancel = true;
                    }
                    return;
                }
                if (_currAction == Action.Delete) {
                    ToggleDeleteAction(true);
                    var checkedTagFilterSets = _deleteTFSSelector.GetCheckedTagFilterSets();
                    DeletionCount = checkedTagFilterSets.Count();
                    TagFilterSetContext.MainContext.TagFilterSets.RemoveRange(checkedTagFilterSets);
                    TagFilterSetContext.MainContext.SaveChanges();
                    ToggleDeleteAction(false);
                }
            };

            CloseButtonClick += (ContentDialog _, ContentDialogButtonClickEventArgs args) => {
                if (_isInAction) args.Cancel = true;
            };
            _inputValidation.InputTextBox.TextChanged += (_, _) => { IsPrimaryButtonEnabled = _inputValidation.InputTextBox.Text.Length != 0;  };
            _inputValidation.InputTextBox.MaxLength = TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN;
            _deleteTFSSelector.RegisterPropertyChangedCallback(
                TFSSelector.AnyCheckedProperty,
                (_, _) => { IsPrimaryButtonEnabled = _deleteTFSSelector.AnyChecked; }
            );

            Loaded += (_, _) => _inputValidation.ClearErrorMsg();
        }

        internal void Init(ObservableCollection<TagFilterSet> collection) {
            _deleteTFSSelector.Init(collection);
        }

        private void ToggleDeleteAction(bool toggle) {
            _isInAction = toggle;
            if (toggle) {
                IsPrimaryButtonEnabled = false;
            }
            IsEnabled = !toggle;
            ActionProgressBar.Visibility = toggle ? Visibility.Visible : Visibility.Collapsed;
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
            if (MainStackPanel.Children.Count > 1) {
                MainStackPanel.Children.RemoveAt(0);
            }
            switch (action) {
                case Action.Create:
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Create").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Create").ValueAsString;
                    IsPrimaryButtonEnabled = false;
                    _inputValidation.AddValidator(CheckDuplicate);
                    _inputValidation.InputTextBox.Text = "";
                    MainStackPanel.Children.Insert(0, _inputValidation);
                    break;
                case Action.Rename:
                    ArgumentNullException.ThrowIfNull(oldName);
                    _oldName = oldName;
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Rename").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Rename").ValueAsString;
                    IsPrimaryButtonEnabled = true;
                    _inputValidation.AddValidator(CheckDuplicate);
                    _inputValidation.AddValidator(CheckSameName);
                    _inputValidation.InputTextBox.Text = oldName;
                    _inputValidation.InputTextBox.SelectAll();
                    MainStackPanel.Children.Insert(0, _inputValidation);
                    break;
                case Action.Delete:
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Delete").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Delete").ValueAsString;
                    IsPrimaryButtonEnabled = _deleteTFSSelector.AnyChecked;
                    MainStackPanel.Children.Insert(0, _deleteTFSSelector);
                    break;
            }
        }

        internal string GetInputText() {
            return _inputValidation.InputTextBox.Text;
        }
    }
}
