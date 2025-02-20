using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class TagFilterController(HitomiContext context) : ControllerBase {
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TagFilter> GetTagFilter(int id) {
            TagFilter? result = context.TagFilters.Find(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<TagFilter>> GetTagFilters() {
            return Ok(context.TagFilters.AsNoTracking());
        }
    }
}
