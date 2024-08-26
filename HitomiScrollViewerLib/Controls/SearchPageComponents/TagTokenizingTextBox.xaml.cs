using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TagTokenizingTextBox : TokenizingTextBox, INotifyPropertyChanged {
        private const int MAX_SUGGESTION_NUM = 8;

        public event PropertyChangedEventHandler PropertyChanged;

        private double _tokenTextBlockMaxWidth;
        public double TokenTextBlockMaxWidth {
            get => _tokenTextBlockMaxWidth;
            set {
                if (_tokenTextBlockMaxWidth != value) {
                    _tokenTextBlockMaxWidth = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Category Category { get; set; }
        public ObservableCollection<Tag> SelectedTags { get; private set; } = [];

        public TagTokenizingTextBox() {
            InitializeComponent();

            SizeChanged += (object sender, SizeChangedEventArgs e) => {
                TokenTextBlockMaxWidth = e.NewSize.Width - 44;
            };
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private IEnumerable<Tag> GetSuggestionTags() {
            return
                HitomiContext.Main.Tags
                .Where(tag => tag.Category == Category && tag.Value.StartsWith(Text))
                .OrderByDescending(tag => tag.GalleryCount)
                .Take(MAX_SUGGESTION_NUM);
        }

        private void TokenizingTextBox_GotFocus(object _0, RoutedEventArgs _1) {
            SuggestedItemsSource = GetSuggestionTags();
        }

        private void TokenizingTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
                SuggestedItemsSource = GetSuggestionTags();
            }
        }

        private void TokenizingTextBox_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args) {
            if (args.TokenText != null) {
                args.Item = Entities.Tag.GetTag(args.TokenText, Category);
            }
        }
    }
}
