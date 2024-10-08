using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TagTokenizingTextBoxVM : DQObservableObject {
        private const int MAX_SUGGESTION_NUM = 8;

        [ObservableProperty]
        private string _text = "";

        public TagCategory Category { get; }
        public ObservableCollection<Tag> SelectedTags { get; set; } = [];
        private HashSet<long> _selectedTagIds = [];

        [ObservableProperty]
        private Tag[] _suggestedItemsSource;

        private readonly HitomiContext _context;

        public TagTokenizingTextBoxVM(HitomiContext context, TagCategory category) {
            _context = context;
            Category = category;
            SelectedTags.CollectionChanged += SelectedTags_CollectionChanged;
        }

        private void SelectedTags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    _selectedTagIds.Add((e.NewItems[0] as Tag).Id);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _selectedTagIds.Remove((e.OldItems[0] as Tag).Id);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _selectedTagIds = [.. SelectedTags.Select(t => t.Id)];
                    break;
            }
        }

        private Tag[] GetSuggestions() {
            IQueryable<Tag> tags = _context.Tags.Where(tag => tag.Category == Category);
            if (Text.Length != 0) {
                tags = tags.Where(tag => tag.Value.StartsWith(Text));
            }
            return [.. tags.OrderByDescending(tag => tag.GalleryCount).Take(MAX_SUGGESTION_NUM)];
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
                Tag tag = _context.GetTag(args.TokenText, Category);
                if (tag == null || _selectedTagIds.Contains(tag.Id)) {
                    args.Cancel = true;
                } else {
                    args.Item = tag;
                }
            } else {
                if (_selectedTagIds.Contains((args.Item as Tag).Id)) {
                    args.Cancel = true;
                }
            }
        }
    }
}
