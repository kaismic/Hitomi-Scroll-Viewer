using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TagTokenizingTextBoxVM(TagCategory category) : DQObservableObject {
        private const int MAX_SUGGESTION_NUM = 8;

        [ObservableProperty]
        private double _tokenTextBlockMaxWidth;
        [ObservableProperty]
        private string _text = "";

        public TagCategory Category { get; } = category;
        public ObservableCollection<Tag> SelectedTags { get; set; } = [];

        [ObservableProperty]
        private Tag[] _suggestedItemsSource;

        private Tag[] GetSuggestions() {

            return Text.Length == 0
                ? [.. HitomiContext.Main.Tags
                    .OrderByDescending(tag => tag.GalleryCount)
                    .Take(MAX_SUGGESTION_NUM)
                ]
                : [.. HitomiContext.Main.Tags
                    .Where(tag => tag.Category == Category && tag.Value.StartsWith(Text))
                    .OrderByDescending(tag => tag.GalleryCount)
                    .Take(MAX_SUGGESTION_NUM)
                ];
        }

        public void TokenizingTextBox_GotFocus(object _0, RoutedEventArgs _1) {
            SuggestedItemsSource = GetSuggestions();
        }

        public void TokenizingTextBox_TextChanged(AutoSuggestBox _0, AutoSuggestBoxTextChangedEventArgs args) {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
                SuggestedItemsSource = GetSuggestions();
            }
        }

        public void TokenizingTextBox_TokenItemAdding(TokenizingTextBox _0, TokenItemAddingEventArgs args) {
            if (args.TokenText != null) {
                args.Item = Tag.GetTag(args.TokenText, Category);
            }
        }

        public void TagTokenizingTextBox_SizeChanged(object _0, SizeChangedEventArgs e) {
            TokenTextBlockMaxWidth = e.NewSize.Width - 44;
        }
    }
}
