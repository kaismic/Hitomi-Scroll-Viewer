using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using static Hitomi_Scroll_Viewer.Tag;

namespace Hitomi_Scroll_Viewer {
    public sealed class TagContainer : Grid {
        private readonly TextBox[] _tagTextBoxes = new TextBox[CATEGORIES.Length];
        private readonly bool _isExclude;
        private readonly StringSplitOptions _splitOption = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        public TagContainer(bool isExclude, string headerText, Color headerColor, int rowSpan) {
            _isExclude = isExclude;

            SetRow(this, 0);
            SetRowSpan(this, rowSpan);
            BorderBrush = new SolidColorBrush(Colors.Black);
            BorderThickness = new Thickness(1);
            Margin = new Thickness(5);

            for (int i = 0; i < CATEGORIES.Length; i++) {
                ColumnDefinitions.Add(new ColumnDefinition());
            }

            //< Border BorderBrush = "Black" BorderThickness = "1" Grid.Row = "0" Grid.Column = "0" Grid.ColumnSpan = "_tagTypes.Length" >
            //    < TextBlock HorizontalAlignment = "Center" VerticalAlignment = "Center" Text = "Include/Exclude" />
            //</ Border >

            Border headerBorder = new() {
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
            };
            SetRow(headerBorder, 0);
            SetColumn(headerBorder, 0);
            SetColumnSpan(headerBorder, CATEGORIES.Length);
            Children.Add(headerBorder);

            TextBlock headerTextBlock = new() {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20,
                FontWeight = new Windows.UI.Text.FontWeight(600),
                Text = headerText,
                Foreground = new SolidColorBrush(headerColor)
            };

            headerBorder.Child = headerTextBlock;

            for (int i = 0; i < CATEGORIES.Length; i++) {
                //< Border BorderBrush = "Black" BorderThickness = "1" Grid.Row = "1" Grid.Column = "i" >
                //    < TextBlock HorizontalAlignment = "Center" VerticalAlignment = "Center" Text = "_tagTypes[i]" />
                //</ Border >
                Border tagHeadingBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(tagHeadingBorder, 1);
                SetColumn(tagHeadingBorder, i);
                Children.Add(tagHeadingBorder);

                TextBlock headingTextBlock = new() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = char.ToUpper(CATEGORIES[i][0]) + CATEGORIES[i][1..]
                };
                tagHeadingBorder.Child = headingTextBlock;

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
            
            // add row definition according to the final number of children
            for (int i = 0; i < Children.Count; i++) {
                RowDefinitions.Add(new RowDefinition());
            }
        }
        
        public async Task ConfirmClearAsync() {
            ContentDialog dialog = new() {
                Title = "Clear all current tags?",
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = XamlRoot
            };
            // TODO call Clear() on button click
            // TODO implement TagContainer to SearchPage
            await dialog.ShowAsync();
        }

        private void Clear() {
            foreach (TextBox tb in _tagTextBoxes) {
                tb.Text = "";
            }
        }

        public string GetTagParameters(int idx) {
            string result = "";
            string[] tags = _tagTextBoxes[idx].Text.Split(Environment.NewLine, _splitOption);
            foreach (string tag in tags) {
                if (_isExclude) {
                    result += '-';
                }
                result += CATEGORIES[idx] + ':' + tag.Replace(' ', '_') + ' ';
            }
            return result;
        }

        public string GetTagStrings(int idx) {
            string result = "";
            string[] tags = _tagTextBoxes[idx].Text.Split(Environment.NewLine, _splitOption);
            foreach (string tag in tags) {
                if (_isExclude) {
                    result += '-';
                }
                result += tag.Replace(' ', '_') + ' ';
            }
            return result;
        }

        public void SetTags(Dictionary<string, string[]> dict) {
            for (int i = 0; i < CATEGORIES.Length; i++) {
                string text = "";
                foreach (string tag in dict[CATEGORIES[i]]) {
                    text += tag + Environment.NewLine;
                }
                _tagTextBoxes[i].Text = text;
            }
        }

        public Dictionary<string, string[]> GetTags() {
            Dictionary<string, string[]> dict = new();
            for (int i = 0; i < CATEGORIES.Length; i++) {
                string[] tags = _tagTextBoxes[i].Text.Split(Environment.NewLine, _splitOption);
                for (int j = 0; j < tags.Length; j++) {
                    tags[j] = tags[j].Replace(' ', '_');
                }
                dict.Add(CATEGORIES[i], tags);
            }
            return dict;
        }
    }
}
