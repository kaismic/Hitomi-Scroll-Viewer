using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class TagController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<Tag>> GetTags(TagCategory category, int count, string? start) {
            IQueryable<Tag> tags = context.Tags.AsNoTracking().Where(tag => tag.Category == category);
            if (start != null && start.Length > 0) {
                tags = tags.Where(tag => tag.Value.StartsWith(start));
            }
            return Ok(tags.OrderByDescending(tag => tag.GalleryCount).Take(count).ToList());
        }
    }
}
