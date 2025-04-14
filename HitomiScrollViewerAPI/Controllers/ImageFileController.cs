using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/image")]
    public class ImageFileController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetImage(int galleryId, int index, string fileExt) {
            Gallery? gallery = context.Galleries.Find(galleryId);
            if (gallery == null) {
                return NotFound($"Gallery with id {galleryId} was not found.");
            }
            context.Entry(gallery).Collection(g => g.GalleryImages).Load();
            GalleryImage? image = gallery.GalleryImages.FirstOrDefault(gi => gi.Gallery.Id == galleryId && gi.Index == index);
            if (image == null) {
                return NotFound($"Image information at the index {index} was not found.");
            }
            try {
                FileStream stream = System.IO.File.OpenRead(Utils.GalleryFileUtil.GetImagePath(gallery, image, fileExt));
                return File(stream, $"image/{fileExt}");
            } catch (FileNotFoundException) {
                return NotFound($"Image file at the index {index} was not found.");
            }
        }
    }
}
