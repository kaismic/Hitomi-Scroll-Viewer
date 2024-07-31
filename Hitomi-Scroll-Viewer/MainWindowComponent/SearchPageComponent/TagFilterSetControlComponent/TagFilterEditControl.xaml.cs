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

        public IEnumerable<string> GetTags(string category) {
            return _tagFilterTextBoxes[CATEGORY_INDEX_MAP[category]].Text
                    .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                    .Select(tag => tag.Replace(' ', '_'));
        }

        public IEnumerable<string> GetTags(int idx) {
            return _tagFilterTextBoxes[idx].Text
                    .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                    .Select(tag => tag.Replace(' ', '_'));
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            using TagFilterSetContext context = new();
            InsertTagFilters(
                context
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedItem)
                .TagFilters
            );
        }
    }
}
