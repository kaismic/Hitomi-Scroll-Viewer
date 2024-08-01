using Hitomi_Scroll_Viewer.DbContexts;
using Hitomi_Scroll_Viewer.Entities;
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

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetControlComponent
{
    public sealed partial class TagFilterSetEditor : StackPanel {
        private readonly TextBox[] _tagFilterTextBoxes = new TextBox[CATEGORIES.Length];

        public TagFilterSetEditor() {
            InitializeComponent();

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++) {
                Grid.SetColumn((FrameworkElement)TagFilterSetControlGrid.Children[i], i);
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                TextBoxesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                Border categoryHeaderBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                Grid.SetRow(categoryHeaderBorder, 0);
                Grid.SetColumn(categoryHeaderBorder, i);
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
                    Padding = new Thickness(0),
                    Height = 200
                };
                Grid.SetRow(_tagFilterTextBoxes[i], 1);
                Grid.SetColumn(_tagFilterTextBoxes[i], i);
                TextBoxesGrid.Children.Add(_tagFilterTextBoxes[i]);
            }
        }

        internal void InsertTagFilters(ICollection<TagFilter> tagFilters) {
            foreach (TagFilter tagFilter in tagFilters) {
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[tagFilter.Category]].Text = string.Join(Environment.NewLine, tagFilter.Tags);
            }
        }

        internal IEnumerable<string> GetTags(string category) {
            return _tagFilterTextBoxes[CATEGORY_INDEX_MAP[category]].Text
                    .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS);
        }

        internal IEnumerable<string> GetTags(int idx) {
            return _tagFilterTextBoxes[idx].Text
                    .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS);
        }

        internal string GetSelectedTagFilterSetName() {
            return (string)TagFilterSetComboBox.SelectedItem;
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (TagFilterSetComboBox.SelectedIndex == -1) {
                RenameButton.IsEnabled = false;
                RenameButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
                return;
            } else {
                RenameButton.IsEnabled = true;
                RenameButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
            using TagFilterSetContext context = new();
            InsertTagFilters(
                context
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedItem)
                .TagFilters
            );
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1) {
            TextBox textBox = new() {
                // TODO
            };
            ContentDialog contentDialog = new() {
                XamlRoot = MainWindow.SearchPage.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Create", // TODO
                CloseButtonText = TEXT_CANCEL,
                Title = new TextBlock() {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = "Enter a name for the new tag filter set" // TODO
                },
                Content = textBox,
                IsPrimaryButtonEnabled = false
            };
            textBox.TextChanged += (_, _) => { contentDialog.IsPrimaryButtonEnabled = textBox.Text.Length != 0; };
            ContentDialogResult cdr = await contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string name = textBox.Text;
            // TODO implementation

            // TODO show infobar
            Trace.WriteLine($"{name} has been created.");
        }

        private async void RenameButton_Click(object _0, RoutedEventArgs _1) {
            string oldName = (string)TagFilterSetComboBox.SelectedItem;
            TextBox textBox = new() {
                Text = oldName,
                SelectionLength = oldName.Length
            };
            ContentDialog contentDialog = new() {
                XamlRoot = MainWindow.SearchPage.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Rename", // TODO
                CloseButtonText = TEXT_CANCEL,
                Title = new TextBlock() {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = "Enter a new name for the current tag filter set" // TODO
                },
                Content = textBox
            };
            textBox.TextChanged += (_, _) => { contentDialog.IsPrimaryButtonEnabled = textBox.Text.Length != 0; };
            ContentDialogResult cdr = await contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string newName = textBox.Text;
            // TODO implementation

            // TODO show infobar
            Trace.WriteLine($"{oldName} has been renamed to {newName}.");
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1) {
            string name = (string)TagFilterSetComboBox.SelectedItem;
            using TagFilterSetContext context = new();
            TagFilterSet tagFilterSet =
                context
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedItem);
            for (int i = 0; i < tagFilterSet.TagFilters.Length; i++) {
                tagFilterSet.TagFilters[i].Tags = GetTags(i);
            }
            context.SaveChanges();
            // TODO show InfoBar
            Trace.WriteLine($"{tagFilterSet.Name} has been saved.");
        }

        private async void DeleteButton_Click(object _0, RoutedEventArgs _1) {
            string name = (string)TagFilterSetComboBox.SelectedItem;
            ContentDialog contentDialog = new() {
                XamlRoot = MainWindow.SearchPage.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Delete", // TODO
                CloseButtonText = TEXT_CANCEL,
                Title = new TextBlock() {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = $"Delete \"{name}\"?" // TODO
                }
            };
            ContentDialogResult cdr = await contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            // TODO implementation

            // TODO show infobar
            Trace.WriteLine($"{name} has been deleted.");
        }
    }
}
