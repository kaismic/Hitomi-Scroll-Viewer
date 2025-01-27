using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class GalleryController : ControllerBase {
        private readonly HitomiContext _context;
        public GalleryController(HitomiContext context) {
            _context = context;
        }
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Gallery>> GetGallery(int id) {
            Console.WriteLine($"GetGallery({id})");
            Gallery? g = await _context.Galleries.FindAsync(id);
            return g == null ? NotFound() : Ok(g);
        }

        [HttpGet("/gallerycount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> GetGalleryCount() {
            return Ok(new JsonResult(await _context.Galleries.CountAsync()));
        }
    }
}
