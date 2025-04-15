using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class GalleryBrowseItem : ComponentBase {
        [Inject] IConfiguration AppConfiguration { get; set; } = default!;
        [Parameter, EditorRequired] public GalleryFullDTO Gallery { get; set; } = default!;
        [Parameter, EditorRequired] public string? Height { get; set; }


        // TODO use IBrowserViewportService to dynamically load thumbnail images
        // https://mudblazor.com/components/breakpointprovider#listening-to-browser-window-breakpoint-changes
        public const int MAX_THUMBNAIL_IMAGES = 3;
        private string _baseImageUrl = "";
        private readonly List<KeyValuePair<TagCategory, List<TagDTO>>> _tagCollections = [];

        protected override void OnInitialized() {
            _baseImageUrl = AppConfiguration["ApiUrl"] + AppConfiguration["ImageFilePath"] + "?galleryId=" + Gallery.Id;
        }

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    List<TagDTO> collection = [.. Gallery.Tags.Where(t => t.Category == category).OrderBy(t => t.Value)];
                    if (collection.Count > 0) {
                        _tagCollections.Add(new(category, collection));
                    }
                }
                StateHasChanged();
            }
        }
    }
}
