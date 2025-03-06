using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class GalleryController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Gallery> GetGallery(int id) {
            Gallery? result = context.Galleries.Find(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<int> GetGalleryCount() {
            return Ok(context.Galleries.AsNoTracking().Count());
        }

        [HttpGet("languages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<GalleryLanguageDTO>> GetGalleryLanguages() {
            return Ok(context.GalleryLanguages.AsNoTracking().Select(l => l.ToDTO()));
        }

        [HttpGet("types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<GalleryTypeDTO>> GetGalleryTypes() {
            return Ok(context.GalleryTypes.AsNoTracking().Select(t => t.ToDTO()));
        }
    }
}
