using Flurl;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class GalleryViewPage {
        [Inject] IConfiguration AppConfiguration { get; set; } = default!;
        [Inject] private GalleryService GalleryService { get; set; } = default!;
        [Parameter] public int GalleryId { get; set; }
        private GalleryFullDTO? _gallery;
        private readonly string[,] _imageUrls = new string[2, Constants.IMAGE_FILE_EXTS.Length];
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                _gallery ??= await GalleryService.GetGalleryFullDTO(GalleryId);
                for (int i = 0; i < _imageUrls.GetLength(0); i++) {
                    for (int j = 0; j < Constants.IMAGE_FILE_EXTS.Length; j++) {
                        _imageUrls[i, j] = new Url(AppConfiguration["ApiUrl"] + AppConfiguration["ImageFilePath"])
                        .SetQueryParams(new {
                            galleryId = GalleryId,
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
