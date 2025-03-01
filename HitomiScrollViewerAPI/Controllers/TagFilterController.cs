using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class TagFilterController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TagFilterDTO> GetTagFilter(int id) {
            TagFilter? result = context.TagFilters.Find(id);
            return result == null ? NotFound() : Ok(result.ToTagFilterDTO());
        }

        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateTagsInTagFilters(int id, [FromBody] IEnumerable<TagDTO> tags) {
            TagFilter? tagFilter = context.TagFilters.Find(id);
            if (tagFilter == null) {
                return NotFound();
            }
            context.Entry(tagFilter).Collection(tf => tf.Tags!).Load();
            tagFilter.Tags!.Clear();
            foreach (TagDTO tagDto in tags) {
                Tag? tag = context.Tags.Find(tagDto.Id);
                if (tag != null) {
                    tagFilter.Tags.Add(tag);
                }
            }
            context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<TagFilterDTO> CreateTagFilter(string name, [FromBody] IEnumerable<TagDTO> tagDtos) {
            List<Tag> tags = [.. tagDtos.Select(t => t.ToTag())];
            TagFilter tagFilter = new() { Name = name, Tags = tags };
            context.Tags.AttachRange(tags);
            context.TagFilters.Add(tagFilter);
            context.SaveChanges();
            return Ok(tagFilter.ToTagFilterDTO());
        }

        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<TagFilterDTO>> GetTagFilters() {
            return Ok(context.TagFilters.AsNoTracking().Select(tf => tf.ToTagFilterDTO()));
        }
    }
}
