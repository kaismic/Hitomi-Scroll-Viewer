using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class SearchFilterView : Grid {

        private SearchFilterVM _viewModel;
        public SearchFilterVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel != null) {
                    return;
                }
                _viewModel = value;
                if (value.GalleryType.GalleryType != Entities.GalleryType.All) {
                    RowDefinitions.Add(new() { Height = GridLength.Auto });
                    TextBlock typeLabel = GetLabelTextBlock();
                    typeLabel.Text = TEXT_TYPE;
                    TextBlock typeName = new() { Text = value.GalleryType.DisplayName };
                    SetColumn(typeLabel, 0);
                    SetColumn(typeName, 1);
                    SetRow(typeLabel, RowDefinitions.Count - 1);
                    SetRow(typeName, RowDefinitions.Count - 1);
                    Children.Add(typeLabel);
                    Children.Add(typeName);
                }
                if (!value.GalleryLanguage.IsAll) {
                    RowDefinitions.Add(new() { Height = GridLength.Auto });
                    TextBlock languageLabel = GetLabelTextBlock();
                    languageLabel.Text = TEXT_LANGUAGE;
                    TextBlock languageValue = new() { Text = value.GalleryLanguage.LocalName };
                    SetColumn(languageLabel, 0);
                    SetColumn(languageValue, 1);
                    SetRow(languageLabel, RowDefinitions.Count - 1);
                    SetRow(languageValue, RowDefinitions.Count - 1);
                    Children.Add(languageLabel);
                    Children.Add(languageValue);
                }
                if (value.SearchTitleText.Length > 0) {
                    RowDefinitions.Add(new() { Height = GridLength.Auto });
                    TextBlock searchTitleLabel = GetLabelTextBlock();
                    searchTitleLabel.Text = "Search Title";
                    TextBlock searchTitleValue = new() { Text = value.SearchTitleText };
                    SetColumn(searchTitleLabel, 0);
                    SetColumn(searchTitleValue, 1);
                    SetRow(searchTitleLabel, RowDefinitions.Count - 1);
                    SetRow(searchTitleValue, RowDefinitions.Count - 1);
                    Children.Add(searchTitleLabel);
                    Children.Add(searchTitleValue);
                }
                foreach (InExcludeTagCollection collection in value.InExcludeTagCollections) {
                    RowDefinitions.Add(new() { Height = GridLength.Auto });
                    TextBlock categoryLabel = GetLabelTextBlock();
                    categoryLabel.Text = collection.CategoryLabel;
                    SetColumn(categoryLabel, 0);
                    SetRow(categoryLabel, RowDefinitions.Count - 1);
                    Children.Add(categoryLabel);
                    bool hasIncludeTags = false;
                    if (collection.IncludeTags.Count > 0) {
                        hasIncludeTags = true;
                        Grid subgrid = GetTagsGrid(true, collection.IncludeTags);
                        Children.Add(subgrid);
                        SetColumn(subgrid, 1);
                        SetRow(subgrid, RowDefinitions.Count - 1);
                    }
                    if (collection.ExcludeTags.Count > 0) {
                        if (hasIncludeTags) {
                            RowDefinitions.Add(new() { Height = GridLength.Auto });
                        }
                        Grid subgrid = GetTagsGrid(false, collection.ExcludeTags);
                        Children.Add(subgrid);
                        SetColumn(subgrid, 1);
                        SetRow(subgrid, RowDefinitions.Count - 1);
                    }
                }
            }
        }

        private static TextBlock GetLabelTextBlock() {
            return new() {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
        }

        private Grid GetTagsGrid(bool isInclude, ICollection<Entities.Tag> tags) {
            Grid grid = new();

            grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });

            TextBlock icon = new() { Text = isInclude ? "✅" : "❌" };
            SetColumn(icon, 0);
            grid.Children.Add(icon);

            WrapPanel tagsWrapPanel = new() { VerticalSpacing = 2, HorizontalSpacing = 4 };
            foreach (var tag in tags) {
                TextBlock tb = new() {
                    Text = tag.Value,
                    Foreground = new SolidColorBrush(Colors.White),
                    Style = Resources["CaptionTextBlockStyle"] as Style
                };
                Border textWrapper = new() {
                    Background = new SolidColorBrush(Colors.DarkGray),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(2),
                    Child = tb
                };
                tagsWrapPanel.Children.Add(textWrapper);
            }
            SetColumn(tagsWrapPanel, 1);
            grid.Children.Add(tagsWrapPanel);

            return grid;
        }

        public SearchFilterView() {
            InitializeComponent();
        }
    }
}
