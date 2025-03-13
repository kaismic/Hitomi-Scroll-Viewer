using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class SearchFilterController(HitomiContext context) : ControllerBase {
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<SearchFilterDTO>> GetAllSearchFilters() {
            IEnumerable<SearchFilter> searchFilters = context.SearchFilters;
            foreach (SearchFilter searchFilter in searchFilters) {
                context.Entry(searchFilter).Reference(sf => sf.Language).Load();
                context.Entry(searchFilter).Reference(sf => sf.Type).Load();
                context.Entry(searchFilter).Collection(sf => sf.LabeledTagCollections).Load();
            }
            return Ok(searchFilters.Select(sf => sf.ToDTO()));
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateSearchFilter([FromBody] SearchFilterDTO dto) {
            SearchFilter entity = dto.ToEntity();
            GalleryLanguage? l = context.GalleryLanguages.Find(dto.Language.Id);
            if (l == null) {
                return NotFound("Language id is not valid.");
            }
            GalleryType? t = context.GalleryTypes.Find(dto.Type.Id);
            if (t == null) {
                return NotFound("Type id is not valid.");
            }
            entity.Language = l;
            entity.Type = t;
            context.SearchFilters.Add(entity);
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
