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

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent
{
    public sealed partial class TagFilterSetEditor : Grid {
        private readonly TextBox[] _tagFilterTextBoxes = new TextBox[CATEGORIES.Length];

        public TagFilterSetEditor()
        {
            InitializeComponent();

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++)
            {
                FrameworkElement elem = TagFilterSetControlGrid.Children[i] as FrameworkElement;
                SetColumn(elem, i);
                if (elem is Button button) {
                    button.Width = 64;
                    button.Height = 64;
                }
            }

            for (int i = 0; i < CATEGORIES.Length; i++)
            {
                TextBoxesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < CATEGORIES.Length; i++)
            {
                Border categoryHeaderBorder = new()
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(categoryHeaderBorder, 0);
                SetColumn(categoryHeaderBorder, i);
                TextBoxesGrid.Children.Add(categoryHeaderBorder);

                TextBlock categoryHeader = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = char.ToUpper(CATEGORIES[i][0]) + CATEGORIES[i][1..]
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tagFilterTextBoxes[i] = new()
                {
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

            using TagFilterSetContext context = new();
            // TODO figure out why combobox doesn't update
            TagFilterSetComboBox.ItemsSource = context.TagFilterSets.Local.ToObservableCollection();
            Trace.WriteLine("TagFilterSets:");
            foreach (var item in context.TagFilterSets)
            {
                Trace.WriteLine(item.Name);
            }
        }

        internal void InsertTagFilters(ICollection<TagFilter> tagFilters)
        {
            foreach (TagFilter tagFilter in tagFilters)
            {
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[tagFilter.Category]].Text = string.Join(Environment.NewLine, tagFilter.Tags);
            }
        }

        internal List<string> GetTags(string category)
        {
            return
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[category]]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        internal List<string> GetTags(int idx)
        {
            return
                _tagFilterTextBoxes[idx]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        internal string GetSelectedTagFilterSetName()
        {
            return ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name;
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagFilterSetComboBox.SelectedIndex == -1)
            {
                RenameButton.IsEnabled = false;
                RenameButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
                return;
            }
            else
            {
                RenameButton.IsEnabled = true;
                RenameButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
            using TagFilterSetContext context = new();
            InsertTagFilters(
                context
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name)
                .TagFilters
            );
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1)
        {
            TextBox textBox = new();
            TextBlock errorMsgTextBlock = new() {
                Foreground = new SolidColorBrush(Colors.Red)
            };
            StackPanel contentPanel = new() {
                Orientation = Orientation.Vertical
            };
            contentPanel.Children.Add(textBox);
            contentPanel.Children.Add(errorMsgTextBlock);

            ContentDialog contentDialog = new()
            {
                XamlRoot = MainWindow.SearchPage.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Create", // TODO
                CloseButtonText = TEXT_CANCEL,
                Title = new TextBlock()
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = "Enter a name for the new tag filter set" // TODO
                },
                Content = contentPanel,
                IsPrimaryButtonEnabled = false
            };
            textBox.TextChanged += (_, _) => {
                contentDialog.IsPrimaryButtonEnabled = textBox.Text.Length != 0;
                errorMsgTextBlock.Text = "";
            };
            using TagFilterSetContext context = new();
            contentDialog.PrimaryButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => {
                string name = textBox.Text;
                if (context.TagFilterSets.Any(tagFilterSet => tagFilterSet.Name == name)) {
                    errorMsgTextBlock.Text = $"\"{name}\" already exists."; // TODO
                    args.Cancel = true;
                    return;
                }
            };
            ContentDialogResult cdr = await contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string name = textBox.Text;
            TagFilterSet tagFilterSet = new() { Name = name };
            for (int i = 0; i < CATEGORIES.Length; i++) {
                tagFilterSet.TagFilters[i].Tags = GetTags(i);
            }
            context.TagFilterSets.Add(tagFilterSet);
            context.SaveChanges();
            // TODO show infobar
            Trace.WriteLine($"{name} has been created.");
        }

        private async void RenameButton_Click(object _0, RoutedEventArgs _1)
        {
            string oldName = ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name;
            TextBox textBox = new()
            {
                Text = oldName,
                SelectionLength = oldName.Length
            };
            ContentDialog contentDialog = new()
            {
                XamlRoot = MainWindow.SearchPage.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Rename", // TODO
                CloseButtonText = TEXT_CANCEL,
                Title = new TextBlock()
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = "Enter a new name for the current tag filter set" // TODO
                },
                Content = textBox
            };
            textBox.TextChanged += (_, _) => { contentDialog.IsPrimaryButtonEnabled = textBox.Text.Length != 0; };
            ContentDialogResult cdr = await contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary)
            {
                return;
            }
            string newName = textBox.Text;
            // TODO implementation

            // TODO show infobar
            Trace.WriteLine($"{oldName} has been renamed to {newName}.");
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1)
        {
            using TagFilterSetContext context = new();
            TagFilterSet tagFilterSet =
                context
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name);
            for (int i = 0; i < tagFilterSet.TagFilters.Count; i++)
            {
                tagFilterSet.TagFilters[i].Tags = GetTags(i);
            }
            context.SaveChanges();
            // TODO show InfoBar
            Trace.WriteLine($"{tagFilterSet.Name} has been saved.");
        }

        private async void DeleteButton_Click(object _0, RoutedEventArgs _1)
        {
            string name = ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name;
            ContentDialog contentDialog = new()
            {
                XamlRoot = MainWindow.SearchPage.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Delete", // TODO
                CloseButtonText = TEXT_CANCEL,
                Title = new TextBlock()
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = $"Delete \"{name}\"?" // TODO
                }
            };
            ContentDialogResult cdr = await contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary)
            {
                return;
            }
            // TODO implementation

            // TODO show infobar
            Trace.WriteLine($"{name} has been deleted.");
        }
    }
}
