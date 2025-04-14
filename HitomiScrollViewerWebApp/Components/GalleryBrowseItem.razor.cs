using Flurl;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class GalleryBrowseItem : ComponentBase {
        [Inject] IConfiguration AppConfiguration { get; set; } = default!;
        [Parameter, EditorRequired] public GalleryFullDTO Gallery { get; set; } = default!;
        //[Parameter] public string? Style { get; set; }
        //[Parameter] public string? Class { get; set; }
        [Parameter, EditorRequired] public string? Height { get; set; }


        public const int MAX_THUMBNAIL_IMAGES = 3;
        private readonly string[,] _imageUrls = new string[MAX_THUMBNAIL_IMAGES, Constants.IMAGE_FILE_EXTS.Length];
        private readonly List<KeyValuePair<TagCategory, List<TagDTO>>> _tagCollections = [];
        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    List<TagDTO> collection = [.. Gallery.Tags.Where(t => t.Category == category).OrderBy(t => t.Value)];
                    if (collection.Count > 0) {
                        _tagCollections.Add(new(category, collection));
                    }
                }
                // TODO use IBrowserViewportService to dynamically load thumbnail images
                // https://mudblazor.com/components/breakpointprovider#listening-to-browser-window-breakpoint-changes
                for (int i = 0; i < _imageUrls.GetLength(0); i++) {
                    for (int j = 0; j < Constants.IMAGE_FILE_EXTS.Length; j++) {
                        _imageUrls[i,j] = new Url(AppConfiguration["ApiUrl"] + AppConfiguration["ImageFilePath"])
                        .SetQueryParams(new {
                            galleryId = Gallery.Id,
                            index = i + 1,
                            fileExt = Constants.IMAGE_FILE_EXTS[j]
                        }).ToString();
                    }
                }
                StateHasChanged();
            }
        }
    }
}
