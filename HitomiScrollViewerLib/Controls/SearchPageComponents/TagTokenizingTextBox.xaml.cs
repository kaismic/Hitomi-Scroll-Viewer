using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TagTokenizingTextBox : TokenizingTextBox {
        private const int MAX_SUGGESTION_NUM = 5;
        public Category Category { get; set; }
        public ObservableCollection<Tag> SelectedTags { get; private set; } = [];
        public TagTokenizingTextBox() {
            InitializeComponent();
        }

        private void TokenizingTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
            if (sender.Text == null || sender.Text.Length == 0) {
                SuggestedItemsSource = null;
                return;
            }
            SuggestedItemsSource =
                HitomiContext.Main.Tags
                .Where(tag => tag.Category == Category && tag.Value.StartsWith(sender.Text))
                .Take(MAX_SUGGESTION_NUM);
        }
    }
}
