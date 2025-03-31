using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/image")]
    public class ImageFileController(HitomiContext context) : ControllerBase {
        private const string IMAGES_PATH = "images";

        [HttpGet("debug")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DebugAPI() {
            return Ok();
        }
        
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetImage(int galleryId, int index) {
            //Gallery? gallery = context.Galleries.Find(galleryId);
            //if (gallery == null) {
            //    return NotFound($"Gallery with the id {galleryId} was not found.");
            //}
            //context.Entry(gallery).Collection(g => g.GalleryImages);
            //GalleryImage? image = gallery.GalleryImages.FirstOrDefault(gi => gi.Index == index);
            //if (image == null) {
            //    return NotFound($"Image with the index {index} was not found.");
            //}
            //FileStream stream = System.IO.File.OpenRead(Path.Combine(IMAGES_PATH, galleryId.ToString(), image.FullFileName));
            //return File(stream, $"image/{image.FileExtension}");


            string indexFormat = "D" + Math.Floor(Math.Log10(99) + 1);
            FileStream stream = System.IO.File.OpenRead(Path.Combine(IMAGES_PATH, galleryId.ToString(), index.ToString(indexFormat) + ".webp"));
            return File(stream, $"image/webp");
        }
    }
}
