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

        [HttpPost("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<TagFilterDTO> DeleteTagFilters([FromBody] IEnumerable<int> ids) {
            IEnumerable<TagFilter> tagFilters = ids.Select(id => context.TagFilters.Find(id)).Where(tf => tf != null).Cast<TagFilter>(); 
            context.TagFilters.RemoveRange(tagFilters);
            context.SaveChanges();
            return Ok();
        }

        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<TagFilterDTO>> GetTagFilters() {
            return Ok(context.TagFilters.AsNoTracking().Select(tf => tf.ToTagFilterDTO()));
        }

        [HttpPatch("tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateTags(int id, [FromBody] IEnumerable<TagDTO> tags) {
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

        [HttpPatch("name")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateName(int id, [FromBody] string name) {
            TagFilter? tagFilter = context.TagFilters.Find(id);
            if (tagFilter == null) {
                return NotFound();
            }
            tagFilter.Name = name;
            context.SaveChanges();
            return Ok();
        }
    }
}
