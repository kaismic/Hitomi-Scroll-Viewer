using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/gallery")]
    public class GalleryController(HitomiContext context) : ControllerBase {
        [HttpGet("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<DownloadGalleryDTO> GetDownloadGalleryDTO(int id) {
            Gallery? gallery = context.Galleries.Find(id);
            if (gallery == null) {
                return NotFound();
            }
            return Ok(gallery.ToDownloadDTO(context.Entry(gallery).Collection(g => g.Images).Query().Count()));
        }

        [HttpGet("view")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ViewGalleryDTO> GetViewGalleryDTO(int id) {
            Gallery? gallery = context.Galleries.Find(id);
            if (gallery == null) {
                return NotFound();
            }
            context.Entry(gallery).Collection(g => g.Images).Load();
            return Ok(gallery.ToViewDTO());
        }

        [HttpGet("count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<int> GetGalleryCount() {
            return Ok(context.Galleries.AsNoTracking().Count());
        }

        /// <summary>
        /// <paramref name="pageIndex"/> is 0-based 
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="itemsPerPage"></param>
        /// <returns></returns>
        [HttpGet("browse-galleries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<BrowseGalleryDTO>> GetBrowseGalleryDTOs(int pageIndex, int itemsPerPage) {
            if (pageIndex < 0) {
                return BadRequest("Page index must be greater than or equal to 0.");
            }
            if (itemsPerPage <= 0) {
                return BadRequest("Items per page must be greater than 0.");
            }
            return Ok(
                context.Galleries
                .AsNoTracking()
                .Skip(pageIndex * itemsPerPage)
                .Take(itemsPerPage)
                .Include(g => g.Language)
                .Include(g => g.Type)
                .Include(g => g.Tags)
                .Include(g => g.Images)
                .Select(g => g.ToBrowseDTO())
            );
        }
    }
}
