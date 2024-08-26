using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class GLASBWrapper : StackPanel {
        private const int MAX_SUGGESTION_NUM = 5;
        public GalleryLanguage SelectedGL { get; private set; }

        public GLASBWrapper() {
            InitializeComponent();
        }

        private IEnumerable<GalleryLanguage> GetSuggestions() {
            return
                HitomiContext.Main.GalleryLanguages
                .Where(gl => gl.DisplayName.StartsWith(AutoSuggestBox.Text))
                .Take(MAX_SUGGESTION_NUM);
        }

        private void AutoSuggestBox_GotFocus(object _0, RoutedEventArgs _1) {
            AutoSuggestBox.ItemsSource = GetSuggestions();
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox _0, AutoSuggestBoxTextChangedEventArgs args) {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
                AutoSuggestBox.ItemsSource = GetSuggestions();
            }
            if (AutoSuggestBox.Text.Length == 0) {
                SelectedGL = null;
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox _0, AutoSuggestBoxQuerySubmittedEventArgs args) {
            SelectedGL = args.ChosenSuggestion != null ? args.ChosenSuggestion as GalleryLanguage : null;
        }
    }
}
