using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class SearchFilterController(HitomiContext context) : ControllerBase {
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<SearchFilterDTO>> GetAllSearchFilters() {
            return Ok(context.SearchFilters.AsNoTracking().Select(sf => sf.ToDTO()));
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateSearchFilter([FromBody] SearchFilterDTO dto) {
            context.SearchFilters.Add(dto.ToEntity());
            context.SaveChanges();
            return Ok();
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteSearchFilter(int id) {
            SearchFilter? searchFilter = context.SearchFilters.Find(id);
            if (searchFilter == null) {
                return NotFound();
            }
            context.SearchFilters.Remove(searchFilter);
            context.SaveChanges();
            return Ok();
        }
    }
}
