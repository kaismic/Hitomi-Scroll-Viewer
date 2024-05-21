using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.SearchTag;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class TagContainer : Grid {
        /*
         * apparently we can't just use Environment.NewLine as separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */

        private readonly TextBox[] _tagTextBoxes = new TextBox[CATEGORIES.Length];

        public bool IsInclude {
            get => (bool)GetValue(IsIncludeProperty);
            set {
                SetValue(IsIncludeProperty, value);
                if (value) {
                    Header.Text = "Include";
                    Header.Foreground = new SolidColorBrush(Colors.Green);
                } else {
                    Header.Text = "Exclude";
                    Header.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }
        public static readonly DependencyProperty IsIncludeProperty = DependencyProperty.Register(
            nameof(IsInclude),
            typeof(bool),
            typeof(TagContainer),
            null
        );

        public TagContainer() {
            InitializeComponent();

            for (int i = 0; i < CATEGORIES.Length; i++) {
                ColumnDefinitions.Add(new ColumnDefinition());
            }
            SetColumnSpan(HeaderBorder, CATEGORIES.Length);

            for (int i = 0; i < CATEGORIES.Length; i++) {
                Border categoryHeaderBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(categoryHeaderBorder, 1);
                SetColumn(categoryHeaderBorder, i);
                Children.Add(categoryHeaderBorder);

                TextBlock categoryHeader = new() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = char.ToUpper(CATEGORIES[i][0]) + CATEGORIES[i][1..]
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tagTextBoxes[i] = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(0),
                    Height = 200
                };
                SetRow(_tagTextBoxes[i], 2);
                SetColumn(_tagTextBoxes[i], i);
                Children.Add(_tagTextBoxes[i]);
            }
        }

        public void Clear() {
            foreach (TextBox tb in _tagTextBoxes) {
                tb.Text = "";
            }
        }

        public bool IsEmpty() {
            return _tagTextBoxes.All(textBox => textBox.Text.Trim().Length == 0);
        }

        public string GetSearchParameters(int idx) {
            string[] curTags = _tagTextBoxes[idx].Text.Split(NEW_LINE_SEPS, STR_SPLIT_OPTION);
            if (IsInclude) {
                return string.Join(' ', curTags.Select(tag => CATEGORIES[idx] + ':' + tag.Replace(' ', '_')));
            }
            return string.Join(' ', curTags.Select(tag => '-' + CATEGORIES[idx] + ':' + tag.Replace(' ', '_')));
        }

        public string GetHyperlinkDisplayTexts(int idx) {
            string[] curTags = _tagTextBoxes[idx].Text.Split(NEW_LINE_SEPS, STR_SPLIT_OPTION);
            if (IsInclude) {
                return string.Join(' ', curTags.Select(tag => tag.Replace(' ', '_')));
            }
            return string.Join(' ', curTags.Select(tag => '-' + tag.Replace(' ', '_')));
        }

        public void InsertTags(Dictionary<string, HashSet<string>> tagList) {
            for (int i = 0; i < CATEGORIES.Length; i++) {
                _tagTextBoxes[i].Text = string.Join(Environment.NewLine, tagList[CATEGORIES[i]]);
            }
        }

        public Dictionary<string, HashSet<string>> GetTags() {
            Dictionary<string, HashSet<string>> tagList = [];
            for (int i = 0; i < CATEGORIES.Length; i++) {
                HashSet<string> tags = _tagTextBoxes[i].Text
                    .Split(NEW_LINE_SEPS, STR_SPLIT_OPTION)
                    .Select(tag => tag.Replace(' ', '_'))
                    .ToHashSet();
                tagList.Add(CATEGORIES[i], tags);
            }
            return tagList;
        }
    }
}
