using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.SearchPage {
    public partial class TagTokenizingTextBoxVM : ObservableObject {
        private const int MAX_SUGGESTION_NUM = 8;

        [ObservableProperty]
        private double _tokenTextBlockMaxWidth;

        [ObservableProperty]
        private string _text;

        public Category Category { get; set; }
        public ObservableCollection<Tag> SelectedTags { get; set; } = [];

        public IEnumerable<Tag> SuggestedItemsSource { get; private set; }

        private IEnumerable<Tag> GetSuggestions() {
            return
                HitomiContext.Main.Tags
                .Where(tag => tag.Category == Category && tag.Value.StartsWith(Text))
                .OrderByDescending(tag => tag.GalleryCount)
                .Take(MAX_SUGGESTION_NUM);
        }

        public void TokenizingTextBox_GotFocus(object _0, RoutedEventArgs _1) {
            SuggestedItemsSource = GetSuggestions();
        }

        public void TokenizingTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
                SuggestedItemsSource = GetSuggestions();
            }
        }

        public void TokenizingTextBox_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args) {
            if (args.TokenText != null) {
                args.Item = Tag.GetTag(args.TokenText, Category);
            }
        }

        public void TagTokenizingTextBox_SizeChanged(object sender, SizeChangedEventArgs e) {
            TokenTextBlockMaxWidth = e.NewSize.Width - 44;
        }
    }
}
