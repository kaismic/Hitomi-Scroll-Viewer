using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.TagFilterList;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class TagContainer : Grid {
        private static readonly ResourceMap ResourceManager = new ResourceManager().MainResourceMap.GetSubtree("TagContainer");
        private readonly TextBox[] _tagTextBoxes = new TextBox[CATEGORIES.Length];

        private bool _isInclude;
        public bool IsInclude {
            get => _isInclude;
            set {
                _isInclude = value;
                if (value) {
                    Header.Text = ResourceManager.GetValue("HeaderText_Include").ValueAsString;
                    Header.Foreground = new SolidColorBrush(Colors.Green);
                } else {
                    Header.Text = ResourceManager.GetValue("HeaderText_Exclude").ValueAsString;
                    Header.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

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
