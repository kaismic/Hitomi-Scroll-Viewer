using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class TagController(HitomiContext context) : ControllerBase {
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<Tag>> GetTags(TagCategory category, int count, string? start) {
            IQueryable<Tag> tags = context.Tags.AsNoTracking().Where(tag => tag.Category == category);
            if (start != null && start.Length > 0) {
                tags = tags.Where(tag => tag.Value.StartsWith(start));
            }
            return Ok(tags.OrderByDescending(tag => tag.GalleryCount).Take(count).ToList());
        }

        [HttpGet("from-tag-filter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<List<TagDTO>> GetTagsFromTagFilter(int tagFilterId) {
            TagFilter? tagFilter = context.TagFilters.Find(tagFilterId);
            if (tagFilter == null) {
                return NotFound();
            }
            context.Entry(tagFilter).Collection(tf => tf.Tags).Load();
            return Ok(tagFilter.Tags.Select(tag => tag.ToTagDTO()).ToList());
        }
    }
}
