using Hitomi_Scroll_Viewer.DbContexts;
using Hitomi_Scroll_Viewer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.Entities.TagFilter;
using static Hitomi_Scroll_Viewer.Utils;
using static Hitomi_Scroll_Viewer.Resources;
using System.Diagnostics;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class TagFilterSetEditor : Grid {
        public static readonly TagFilterSetContext TagFilterSetContext = new();
        private readonly TextBox[] _tagFilterTextBoxes = new TextBox[CATEGORIES.Length];
        private readonly TagFilterSetEditorContentDialog _contentDialog = new(TagFilterSetContext);

        public TagFilterSetEditor() {
            InitializeComponent();

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++) {
                FrameworkElement elem = TagFilterSetControlGrid.Children[i] as FrameworkElement;
                SetColumn(elem, i);
                if (elem is Button button) {
                    button.Width = 64;
                    button.Height = 64;
                }
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                TextBoxesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                Border categoryHeaderBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(categoryHeaderBorder, 0);
                SetColumn(categoryHeaderBorder, i);
                TextBoxesGrid.Children.Add(categoryHeaderBorder);

                TextBlock categoryHeader = new() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = char.ToUpper(CATEGORIES[i][0]) + CATEGORIES[i][1..]
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tagFilterTextBoxes[i] = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(0)
                };
                SetRow(_tagFilterTextBoxes[i], 1);
                SetColumn(_tagFilterTextBoxes[i], i);
                TextBoxesGrid.Children.Add(_tagFilterTextBoxes[i]);
            }

            //_tagFilterSetContext.Database.EnsureDeleted();
            TagFilterSetContext.Database.EnsureCreated();
            TagFilterSetContext.TagFilterSets.Load();
            TagFilterSetComboBox.ItemsSource = TagFilterSetContext.TagFilterSets.Local.ToObservableCollection();

            Loaded += TagFilterSetEditor_Loaded;
        }

        private void TagFilterSetEditor_Loaded(object sender, RoutedEventArgs e) {
            _contentDialog.XamlRoot = MainWindow.SearchPage.XamlRoot;
            Loaded -= TagFilterSetEditor_Loaded;
        }

        internal void InsertTagFilters(ICollection<TagFilter> tagFilters) {
            foreach (TagFilter tagFilter in tagFilters) {
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[tagFilter.Category]].Text = string.Join(Environment.NewLine, tagFilter.Tags);
            }
        }

        internal List<string> GetTags(string category) {
            return
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[category]]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        internal List<string> GetTags(int idx) {
            return
                _tagFilterTextBoxes[idx]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        internal string GetSelectedTagFilterSetName() {
            return (string)TagFilterSetComboBox.SelectedValue;
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (TagFilterSetComboBox.SelectedIndex == -1) {
                RenameButton.IsEnabled = SaveButton.IsEnabled = DeleteButton.IsEnabled = false;
                return;
            }
            RenameButton.IsEnabled = SaveButton.IsEnabled = DeleteButton.IsEnabled = true;
            InsertTagFilters(
                TagFilterSetContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedValue)
                .TagFilters
            );
        }

        private class TagFilterSetEditorContentDialog : ContentDialog {
            private readonly TextBlock _titleTextBlock = new() {
                TextWrapping = TextWrapping.WrapWholeWords
            };
            private readonly TextBox _textBox = new();
            private readonly TextBlock _errorMsgTextBlock = new() {
                Foreground = new SolidColorBrush(Colors.Red)
            };
            private readonly StackPanel _contentPanel = new() {
                Orientation = Orientation.Vertical
            };
            internal enum DialogType {
                Create, Rename, Delete
            }
            private string _oldName;

            public TagFilterSetEditorContentDialog(TagFilterSetContext tagFilterSetContext) {
                DefaultButton = ContentDialogButton.Primary;
                CloseButtonText = TEXT_CANCEL;
                Title = _titleTextBlock;
                _contentPanel.Children.Add(_textBox);
                _contentPanel.Children.Add(_errorMsgTextBlock);
                _textBox.TextChanged += (_, _) => {
                    IsPrimaryButtonEnabled = _textBox.Text.Length != 0;
                    _errorMsgTextBlock.Text = "";
                };
                PrimaryButtonClick += (ContentDialog _, ContentDialogButtonClickEventArgs args) => {
                    // Delete
                    if (Content == null) {
                        return;
                    }
                    string newName = _textBox.Text;
                    // Rename or Create
                    if (_oldName == newName) {
                        _errorMsgTextBlock.Text = "Cannot rename to the same name."; // TODO
                        args.Cancel = true;
                        return;
                    }
                    // Create
                    if (tagFilterSetContext.TagFilterSets.Any(tagFilterSet => tagFilterSet.Name == newName)) {
                        _errorMsgTextBlock.Text = $"\"{newName}\" already exists."; // TODO
                        args.Cancel = true;
                        return;
                    }
                };
            }

            internal void SetDialogType(DialogType dialogType, string title, string primaryButtonText, string oldName = null) {
                _errorMsgTextBlock.Text = "";
                _titleTextBlock.Text = title;
                PrimaryButtonText = primaryButtonText;
                _oldName = oldName;
                if (dialogType == DialogType.Delete) {
                    IsPrimaryButtonEnabled = true;
                    Content = null;
                    return;
                } else {
                    Content = _contentPanel;
                }
                switch (dialogType) {
                    case DialogType.Create:
                        IsPrimaryButtonEnabled = false;
                        _textBox.Text = "";
                        break;
                    case DialogType.Rename:
                        IsPrimaryButtonEnabled = true;
                        ArgumentNullException.ThrowIfNull(oldName);
                        _textBox.Text = oldName;
                        _textBox.SelectAll();
                        break;
                }
            }

            internal string GetTextBoxText() {
                return _textBox.Text;
            }
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1) {
            _contentDialog.SetDialogType(
                TagFilterSetEditorContentDialog.DialogType.Create,
                "Enter a name for the new tag filter set:", // TODO
                "Create" // TODO
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string name = _contentDialog.GetTextBoxText();
            TagFilterSet tagFilterSet = new() {
                Name = name,
                TagFilters =
                    CATEGORIES
                    .Select(
                        category => new TagFilter() {
                            Category = category,
                            Tags = GetTags(category)
                        }
                    )
                    .ToList()
            };
            TagFilterSetContext.TagFilterSets.Add(tagFilterSet);
            TagFilterSetContext.SaveChanges();
            TagFilterSetComboBox.SelectedItem = tagFilterSet;
            // TODO show infobar
            Trace.WriteLine($"{name} has been created.");
        }

        private async void RenameButton_Click(object _0, RoutedEventArgs _1) {
            string oldName = (string)TagFilterSetComboBox.SelectedValue;
            _contentDialog.SetDialogType(
                TagFilterSetEditorContentDialog.DialogType.Rename,
                "Enter a new name for the current tag filter set:", // TODO
                "Rename", // TODO
                oldName
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string newName = _contentDialog.GetTextBoxText();
            TagFilterSetContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == oldName).Name = newName;
            TagFilterSetContext.SaveChanges();
            // TODO show infobar
            Trace.WriteLine($"{oldName} has been renamed to {newName}.");
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1) {
            TagFilterSet tagFilterSet =
                TagFilterSetContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedValue);
            for (int i = 0; i < tagFilterSet.TagFilters.Count; i++) {
                (tagFilterSet.TagFilters as List<TagFilter>)[i].Tags = GetTags(i);
            }
            TagFilterSetContext.SaveChanges();
            // TODO show InfoBar
            Trace.WriteLine($"{tagFilterSet.Name} has been saved.");
        }

        private async void DeleteButton_Click(object _0, RoutedEventArgs _1) {
            string name = (string)TagFilterSetComboBox.SelectedValue;
            _contentDialog.SetDialogType(
                TagFilterSetEditorContentDialog.DialogType.Delete,
                $"Delete \"{name}\"?", // TODO
                "Delete" // TODO
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagFilterSetContext.TagFilterSets.Remove(TagFilterSetContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == name));
            TagFilterSetContext.SaveChanges();
            foreach (TextBox textBox in _tagFilterTextBoxes) {
                textBox.Text = "";
            }
            // TODO show infobar
            Trace.WriteLine($"{name} has been deleted.");
        }
    }
}
