using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class TagFilterSetEditor : Grid {
        private TagFilterSetEditorVM _viewModel;
        public TagFilterSetEditorVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                for (int i = 0; i < value.TTTextBoxVMs.Length; i++) {
                    _tfsTextBoxes[i].ViewModel = value.TTTextBoxVMs[i];
                }
            }
        }

        private readonly TagTokenizingTextBox[] _tfsTextBoxes = new TagTokenizingTextBox[Entities.Tag.CATEGORY_NUM];

        public TagFilterSetEditor() {
            InitializeComponent();

            for (int i = 0; i < Children.Count; i++) {
                SetRow(Children[i] as FrameworkElement, i);
            }

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++) {
                FrameworkElement child = TagFilterSetControlGrid.Children[i] as FrameworkElement;
                SetColumn(child, i);
                if (child is Button button) {
                    button.Padding = new Thickness(12);
                }
            }

            for (int i = 0; i < LanguageTypeKeywordGrid.Children.Count; i++) {
                SetColumn(LanguageTypeKeywordGrid.Children[i] as FrameworkElement, i);
            }

            for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
                Border categoryHeaderBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(categoryHeaderBorder, 0);
                SetColumn(categoryHeaderBorder, i);
                TTTextBoxesGrid.Children.Add(categoryHeaderBorder);

                TextBlock categoryHeader = new() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = Enum.GetName((Category)i)
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tfsTextBoxes[i] = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(0)
                };
                Border tfsTextBoxWrapper = new() {
                    Child = _tfsTextBoxes[i]
                };
                tfsTextBoxWrapper.SizeChanged += (object _0, SizeChangedEventArgs e) => {
                    _tfsTextBoxes[i].MaxHeight = e.NewSize.Height;
                };
                tfsTextBoxWrapper.Loaded += TfsTextBoxWrapper_Loaded;
                SetRow(tfsTextBoxWrapper, 1);
                SetColumn(tfsTextBoxWrapper, i);
                TTTextBoxesGrid.Children.Add(tfsTextBoxWrapper);
            }
        }

        private void TfsTextBoxWrapper_Loaded(object sender, RoutedEventArgs _1) {
            Border tfsTextBoxWrapper = sender as Border;
            tfsTextBoxWrapper.Loaded -= TfsTextBoxWrapper_Loaded;
            (tfsTextBoxWrapper.Child as TagTokenizingTextBox).MaxHeight = tfsTextBoxWrapper.ActualHeight;
        }
    }
}
