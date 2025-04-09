using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class GalleryBrowseItem : ComponentBase {
        [Inject] ApiUrlService ApiUrlService { get; set; } = default!;
        [Parameter, EditorRequired] public GalleryFullDTO Gallery { get; set; } = default!;
        [Parameter] public string? Style { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Width { get; set; }
        [Parameter] public string? Height { get; set; }


        private const int MAX_THUMBNAIL_IMAGES = 3;
        private readonly List<string> _imageUrls = [];
        private readonly List<KeyValuePair<TagCategory, List<TagDTO>>> _tagCollections = [];
        protected override void OnAfterRender(bool firstRender) {
            base.OnAfterRender(firstRender);
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                List<TagDTO> collection = [.. Gallery.Tags.Where(t => t.Category == category).OrderBy(t => t.Value)];
                if (collection.Count > 0) {
                    _tagCollections.Add(new(category, collection));
                }
            }
            // TODO variable image thumbnail number based on current item width?
            for (int i = 1; i <= MAX_THUMBNAIL_IMAGES; i++) {
                _imageUrls.Add(ApiUrlService.GetImageUrl(Gallery.Id, i));
            }
            StateHasChanged();
        }
    }
}
