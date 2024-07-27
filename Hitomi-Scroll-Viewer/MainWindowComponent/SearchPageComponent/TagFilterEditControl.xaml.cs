using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.TagFilter;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class TagFilterEditControl : Grid {
        private readonly TextBox[] _tagFilterTextBoxes = new TextBox[CATEGORIES.Length];

        public TagFilterEditControl() {
            InitializeComponent();

            for (int i = 0; i < CATEGORIES.Length; i++) {
                ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                Border categoryHeaderBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(categoryHeaderBorder, 0);
                SetColumn(categoryHeaderBorder, i);
                Children.Add(categoryHeaderBorder);

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
                SetRow(_tagFilterTextBoxes[i], 1);
                SetColumn(_tagFilterTextBoxes[i], i);
                Children.Add(_tagFilterTextBoxes[i]);
            }
        }

        public void Clear() {
            foreach (TextBox tb in _tagFilterTextBoxes) {
                tb.Text = "";
            }
        }

        public void InsertTags(Dictionary<string, HashSet<string>> tagList) {
            for (int i = 0; i < CATEGORIES.Length; i++) {
                _tagFilterTextBoxes[i].Text = string.Join(Environment.NewLine, tagList[CATEGORIES[i]]);
            }
        }

        public Dictionary<string, HashSet<string>> GetTags() {
            Dictionary<string, HashSet<string>> tagList = [];
            for (int i = 0; i < CATEGORIES.Length; i++) {
                HashSet<string> tags = _tagFilterTextBoxes[i].Text
                    .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                    .Select(tag => tag.Replace(' ', '_'))
                    .ToHashSet();
                tagList.Add(CATEGORIES[i], tags);
            }
            return tagList;
        }
    }
}
