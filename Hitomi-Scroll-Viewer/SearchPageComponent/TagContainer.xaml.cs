using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using static Hitomi_Scroll_Viewer.Tag;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class TagContainer : Grid {
        /*
         * apparently we can't just use Environment.NewLine as separator
         * because of this disgusting TextBox bug which somehow force-converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] newlineSep = new[] { Environment.NewLine, "\r" };

        private readonly TextBox[] _tagTextBoxes = new TextBox[CATEGORIES.Length];
        private readonly bool _isExclude;
        private readonly StringSplitOptions _splitOption = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        public TagContainer(bool isExclude, string headerText, Color headerColor) {
            InitializeComponent();

            _isExclude = isExclude;

            for (int i = 0; i < CATEGORIES.Length; i++) {
                ColumnDefinitions.Add(new ColumnDefinition());
            }

            //< Border BorderBrush = "Black" BorderThickness = "1" Grid.Row = "0" Grid.Column = "0" Grid.ColumnSpan = "CATEGORIES.Length" >
            //    < TextBlock HorizontalAlignment = "Center" VerticalAlignment = "Center" Text = "Include/Exclude" />
            //</ Border >

            SetColumnSpan(HeaderBorder, CATEGORIES.Length);

            Header.Text = headerText;
            Header.Foreground = new SolidColorBrush(headerColor);

            for (int i = 0; i < CATEGORIES.Length; i++) {
                //< Border BorderBrush = "Black" BorderThickness = "1" Grid.Row = "1" Grid.Column = "i" >
                //    < TextBlock HorizontalAlignment = "Center" VerticalAlignment = "Center" Text = "CATEGORIES[i]" />
                //</ Border >
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

                //< TextBox BorderBrush = "Black" BorderThickness = "1" Grid.Row = "2" Grid.Column = "i" AcceptsReturn = "True" TextWrapping = "Wrap" Height = "200" CornerRadius = "0" ></ TextBox >
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
            
            // add 3 row definitions
            for (int i = 0; i < 3; i++) {
                RowDefinitions.Add(new RowDefinition());
            }
        }

        public void Clear() {
            foreach (TextBox tb in _tagTextBoxes) {
                tb.Text = "";
            }
        }

        public string GetTagParameters(int idx) {
            string result = "";
            string[] curTags = _tagTextBoxes[idx].Text.Split(newlineSep, _splitOption);
            string[] globalTags = SearchPage.GetGlobalTag(CATEGORIES[idx], _isExclude);
            foreach (string tag in curTags.Union(globalTags)) {
                if (_isExclude) {
                    result += '-';
                }
                result += CATEGORIES[idx] + ':' + tag.Replace(' ', '_') + ' ';
            }
            return result;
        }

        public string GetTagStrings(int idx) {
            string result = "";
            string[] curTags = _tagTextBoxes[idx].Text.Split(newlineSep, _splitOption);
            string[] globalTags = SearchPage.GetGlobalTag(CATEGORIES[idx], _isExclude);
            foreach (string tag in curTags.Union(globalTags)) {
                if (_isExclude) {
                    result += '-';
                }
                result += tag.Replace(' ', '_') + ' ';
            }
            return result;
        }

        public void InsertTags(Dictionary<string, string[]> tagList) {
            for (int i = 0; i < CATEGORIES.Length; i++) {
                string text = "";
                foreach (string tag in tagList[CATEGORIES[i]]) {
                    text += tag + Environment.NewLine;
                }
                _tagTextBoxes[i].Text = text;
            }
        }

        public Dictionary<string, string[]> GetTags() {
            Dictionary<string, string[]> tagList = new();
            for (int i = 0; i < CATEGORIES.Length; i++) {
                string[] tags = _tagTextBoxes[i].Text.Split(newlineSep, _splitOption);
                for (int j = 0; j < tags.Length; j++) {
                    tags[j] = tags[j].Replace(' ', '_');
                }
                tagList.Add(CATEGORIES[i], tags);
            }
            return tagList;
        }
    }
}
