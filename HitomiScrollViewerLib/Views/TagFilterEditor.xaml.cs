using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class TagFilterEditor : Grid {
        private TagFilterEditorVM _viewModel;
        public TagFilterEditorVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                for (int i = 0; i < value.TttVMs.Length; i++) {
                    _tagTokenizingTextBoxes[i].ViewModel = value.TttVMs[i];
                }
            }
        }

        private readonly TagTokenizingTextBox[] _tagTokenizingTextBoxes = new TagTokenizingTextBox[Entities.Tag.TAG_CATEGORIES.Length];

        public TagFilterEditor() {
            InitializeComponent();

            for (int i = 0; i < Children.Count; i++) {
                SetRow(Children[i] as FrameworkElement, i);
            }

            for (int i = 0; i < CrudOperationGrid.Children.Count; i++) {
                FrameworkElement child = CrudOperationGrid.Children[i] as FrameworkElement;
                SetColumn(child, i);
                if (child is Button button) {
                    button.Padding = new Thickness(12);
                }
            }

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
                    Padding = new Thickness(0)
                };
                Border wrapper = new() {
                    Child = _tagTokenizingTextBoxes[i]
                };
                int localIdx = i;
                wrapper.SizeChanged += (object _0, SizeChangedEventArgs e) => {
                    _tagTokenizingTextBoxes[localIdx].MaxHeight = e.NewSize.Height;
                };
                wrapper.Loaded += Wrapper_Loaded;
                SetRow(wrapper, 1);
                SetColumn(wrapper, i);
                TextBoxesGrid.Children.Add(wrapper);
            }
        }

        private void Wrapper_Loaded(object sender, RoutedEventArgs _1) {
            Border tfsTextBoxWrapper = sender as Border;
            tfsTextBoxWrapper.Loaded -= Wrapper_Loaded;
            (tfsTextBoxWrapper.Child as TagTokenizingTextBox).MaxHeight = tfsTextBoxWrapper.ActualHeight;
        }
    }
}
