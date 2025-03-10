using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/queryconfig")]
    public class QueryConfigurationController(HitomiContext context) : ControllerBase {
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SearchQueryConfiguration> GetSearchQueryConfiguration() {
            return Ok(context.SearchQueryConfigurations.AsNoTracking().First());
        }

        [HttpPatch("search/include-tagfilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateIncludeTagFilters(int id, [FromBody] IEnumerable<int> tagFilterIds) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            config.SelectedIncludeTagFilterIds = tagFilterIds;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/exclude-tagfilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateExcludeTagFilters(int id, [FromBody] IEnumerable<int> tagFilterIds) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            config.SelectedExcludeTagFilterIds = tagFilterIds;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/selected-tagfilter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSelectedTagFilter(int id, int tagFilterId) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            config.SelectedTagFilter = context.TagFilters.Find(tagFilterId);
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/language")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateLanguage(int id, int languageId) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            GalleryLanguage? language = context.GalleryLanguages.Find(languageId);
            if (language == null) {
                return NotFound();
            }
            config.GalleryLanguage = language;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateType(int id, int typeId) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            GalleryType? type = context.GalleryTypes.Find(typeId);
            if (type == null) {
                return NotFound();
            }
            config.GalleryType = type;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/searchKeywordText")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSearchKeywordText(int id, string searchKeywordText) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            config.SearchKeywordText = searchKeywordText;
            context.SaveChanges();
            return Ok();
        }

        [HttpGet("browse")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<BrowseQueryConfiguration> GetBrowseQueryConfiguration() {
            return Ok(context.BrowseQueryConfigurations.AsNoTracking().First());
        }
    }
}
