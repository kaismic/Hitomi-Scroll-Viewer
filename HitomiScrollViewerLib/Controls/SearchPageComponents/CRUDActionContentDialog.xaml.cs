using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
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

        public CRUDActionContentDialog() {
            InitializeComponent();
            DefaultButton = ContentDialogButton.Primary;
            CloseButtonText = TEXT_CANCEL;


            // TODO turn these into InputValidation
            LengthDisplayTextBlock.Text = $"0/{TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN}";
            InputTextBox.MaxLength = TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN;
            InputTextBox.TextChanged += (_, _) => {
                LengthDisplayTextBlock.Text = $"{InputTextBox.Text.Length}/{TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN}";
                IsPrimaryButtonEnabled = InputTextBox.Text.Length != 0;
                ErrorMsgTextBlock.Text = "";
            };
            PrimaryButtonClick += (ContentDialog _, ContentDialogButtonClickEventArgs args) => {
                switch (_currAction) {
                    case Action.Create:
                        string name = InputTextBox.Text;
                        if (TagFilterSetContext.MainContext.TagFilterSets.Any(tagFilterSet => tagFilterSet.Name == name)) {
                            ErrorMsgTextBlock.Text = string.Format(
                                _resourceMap.GetValue("Error_Message_Duplicate").ValueAsString,
                                name
                            );
                            args.Cancel = true;
                        }
                        break;
                    case Action.Rename:
                        string newName = InputTextBox.Text;
                        if (_oldName == newName) {
                            ErrorMsgTextBlock.Text = _resourceMap.GetValue("Error_Message_SameName").ValueAsString;
                            args.Cancel = true;
                        }
                        break;
                    case Action.Delete:
                        break;
                }
            };
        }

        internal void SetDialogAction(Action action, string oldName = null, TagFilterSetSelector tagFilterSetSelector = null) {
            _currAction = action;
            ErrorMsgTextBlock.Text = "";
            switch (action) {
                case Action.Create:
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Create").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Create").ValueAsString;
                    IsPrimaryButtonEnabled = false;
                    //Content = ; // TODO InputValidation
                    InputTextBox.Text = "";
                    break;
                case Action.Rename:
                    ArgumentNullException.ThrowIfNull(oldName);
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Rename").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Rename").ValueAsString;
                    IsPrimaryButtonEnabled = true;
                    //Content = ; // TODO InputValidation
                    InputTextBox.Text = _oldName = oldName;
                    InputTextBox.SelectAll();
                    break;
                case Action.Delete:
                    ArgumentNullException.ThrowIfNull(tagFilterSetSelector);
                    TitleTextBlock.Text = _resourceMap.GetValue("Title_Delete").ValueAsString;
                    PrimaryButtonText = _resourceMap.GetValue("Text_Delete").ValueAsString;
                    IsPrimaryButtonEnabled = tagFilterSetSelector.AnyChecked;
                    Content = tagFilterSetSelector;
                    break;
            }
        }

        internal string GetText() {
            return InputTextBox.Text;
        }
    }
}
