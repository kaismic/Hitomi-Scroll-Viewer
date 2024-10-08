using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class QueryBuilderView : Grid {
        private QueryBuilderVM _viewModel;
        public QueryBuilderVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                for (int i = 0; i < value.TagTokenizingTBVMs.Length; i++) {
                    _tagTokenizingTextBoxes[i].ViewModel = value.TagTokenizingTBVMs[i];
                }
            }
        }

        private readonly TagTokenizingTextBox[] _tagTokenizingTextBoxes = new TagTokenizingTextBox[Entities.Tag.TAG_CATEGORIES.Length];

        public QueryBuilderView() {
            InitializeComponent();

            for (int i = 0; i < LanguageTypeKeywordGrid.Children.Count; i++) {
                SetColumn(LanguageTypeKeywordGrid.Children[i] as FrameworkElement, i);
            }

            for (int i = 0; i < Entities.Tag.TAG_CATEGORIES.Length; i++) {
                TextBoxesGrid.ColumnDefinitions.Add(new());
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
                    Text = Enum.GetName((TagCategory)i)
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tagTokenizingTextBoxes[i] = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(0),
                };
                
                StackPanel wrapperPanel = new();
                wrapperPanel.Children.Add(_tagTokenizingTextBoxes[i]);

                SetRow(wrapperPanel, 1);
                SetColumn(wrapperPanel, i);
                TextBoxesGrid.Children.Add(wrapperPanel);
            }
        }
    }
}
