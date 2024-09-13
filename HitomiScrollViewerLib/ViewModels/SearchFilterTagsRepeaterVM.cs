using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.ViewModels {
    public class SearchFilterTagsRepeaterVM {
        public string CategoryLabel { get; init; }

        private List<Tag> _includeTags;
        public List<Tag> IncludeTags {
            get => _includeTags;
            init {
                _includeTags = value;
                if (value.Count > 0) {
                    IncludeTagsGridVisibility = Visibility.Visible;
                }
            }
        }

        private List<Tag> _excludeTags;
        public List<Tag> ExcludeTags {
            get => _excludeTags;
            init {
                _excludeTags = value;
                if (value.Count > 0) {
                    ExcludeTagsGridVisibility = Visibility.Visible;
                }
            }
        }

        public Visibility IncludeTagsGridVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility ExcludeTagsGridVisibility { get; private set; } = Visibility.Collapsed;
    }
}
