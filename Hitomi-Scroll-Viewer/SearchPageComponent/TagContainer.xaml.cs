using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.Tag;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class TagContainer : Grid {
        /*
         * apparently we can't just use Environment.NewLine as separator
         * because of this TextBox bug which somehow force-converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] newlineSep = new[] { Environment.NewLine, "\r" };

        private readonly TextBox[] _tagTextBoxes = new TextBox[CATEGORIES.Length];
        private readonly bool _isExclude;
        private readonly StringSplitOptions _splitOption = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        private readonly SearchPage _sp;
        public TagContainer(SearchPage sp, bool isExclude) {
            InitializeComponent();

            _sp = sp;
            _isExclude = isExclude;

            for (int i = 0; i < 3; i++) {
                RowDefinitions.Add(new RowDefinition());
            }
            for (int i = 0; i < CATEGORIES.Length; i++) {
                ColumnDefinitions.Add(new ColumnDefinition());
            }
            SetColumnSpan(HeaderBorder, CATEGORIES.Length);

            if (isExclude) {
                Header.Text = "Exclude";
                Header.Foreground = new SolidColorBrush(Colors.Red);
            } else {
                Header.Text = "Include";
                Header.Foreground = new SolidColorBrush(Colors.Green);
            }

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

        public string GetTagParameters(int idx) {
            string[] curTags = _tagTextBoxes[idx].Text.Split(newlineSep, _splitOption);
            string[] globalTags = _sp.GetGlobalTag(CATEGORIES[idx], _isExclude);
            if (_isExclude) {
                return string.Join(' ', curTags.Union(globalTags).Select(tag => '-' + CATEGORIES[idx] + ':' + tag.Trim().Replace(' ', '_') + ' '));
            }
            return string.Join(' ', curTags.Union(globalTags).Select(tag => CATEGORIES[idx] + ':' + tag.Trim().Replace(' ', '_') + ' '));
        }

        public string GetTagStrings(int idx) {
            string[] curTags = _tagTextBoxes[idx].Text.Split(newlineSep, _splitOption);
            string[] globalTags = _sp.GetGlobalTag(CATEGORIES[idx], _isExclude);
            if (_isExclude) {
                return string.Join(' ', curTags.Union(globalTags).Select(tag => '-' + tag.Trim().Replace(' ', '_') + ' '));
            }
            return string.Join(' ', curTags.Union(globalTags).Select(tag => tag.Trim().Replace(' ', '_') + ' '));
        }

        public void InsertTags(Dictionary<string, string[]> tagList) {
            for (int i = 0; i < CATEGORIES.Length; i++) {
                _tagTextBoxes[i].Text = string.Join(Environment.NewLine, tagList[CATEGORIES[i]]);
            }
        }

        public Dictionary<string, string[]> GetTags() {
            Dictionary<string, string[]> tagList = new();
            for (int i = 0; i < CATEGORIES.Length; i++) {
                string[] tags = _tagTextBoxes[i].Text.Split(newlineSep, _splitOption);
                tags = tags.Select(tag => tag.Trim().Replace(' ', '_')).ToArray();
                Array.Sort(tags);
                tagList.Add(CATEGORIES[i], tags);
            }
            return tagList;
        }
    }
}
