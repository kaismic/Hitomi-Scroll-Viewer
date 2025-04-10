using HitomiScrollViewerAPI.Services;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/image")]
    public class ImageFileController(HitomiContext context) : ControllerBase {
        [HttpGet("debug")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DebugAPI() {
            return Ok();
        }
        
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetImage(int galleryId, int index) {
            Gallery? gallery = context.Galleries.Find(galleryId);
            if (gallery == null) {
                return NotFound($"Gallery with id {galleryId} was not found.");
            }
            GalleryImage? image = gallery.GalleryImages.FirstOrDefault(gi => gi.Gallery.Id == galleryId && gi.Index == index);
            if (image == null) {
                return NotFound($"Image with the index {index} was not found.");
            }
            FileStream stream = System.IO.File.OpenRead(Utils.GalleryFileUtil.GetImagePath(gallery, image));
            return File(stream, $"image/{image.FileExt}");
        }
    }
}
