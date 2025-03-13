using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class QueryConfigurationController(HitomiContext context) : ControllerBase {
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SearchQueryConfigurationDTO> GetSearchQueryConfiguration() {
            SearchQueryConfiguration config = context.SearchQueryConfigurations.First();
            context.Entry(config).Reference(c => c.SelectedTagFilter).Load();
            context.Entry(config).Reference(c => c.SelectedLanguage).Load();
            context.Entry(config).Reference(c => c.SelectedType).Load();
            return Ok(config.ToDTO());
        }

        [HttpPatch("search/include-tagfilters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSearchIncludeTagFilters(int id, [FromBody] IEnumerable<int> tagFilterIds) {
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
        public ActionResult UpdateSearchExcludeTagFilters(int id, [FromBody] IEnumerable<int> tagFilterIds) {
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
        public ActionResult UpdateSearchSelectedTagFilter(int id, int tagFilterId) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Reference(c => c.SelectedTagFilter).Load();
            if (config.SelectedTagFilter?.Id == tagFilterId) {
                return Ok();
            }
            config.SelectedTagFilter = context.TagFilters.Find(tagFilterId);
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/language")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSearchLanguage(int id, int languageId) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            GalleryLanguage? language = context.GalleryLanguages.Find(languageId);
            if (language == null) {
                return NotFound();
            }
            config.SelectedLanguage = language;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSearchType(int id, int typeId) {
            SearchQueryConfiguration? config = context.SearchQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            GalleryType? type = context.GalleryTypes.Find(typeId);
            if (type == null) {
                return NotFound();
            }
            config.SelectedType = type;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search/SearchKeywordText")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSearchSearchKeywordText(int id, string searchKeywordText) {
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
        public ActionResult<BrowseQueryConfigurationDTO> GetBrowseQueryConfiguration() {
            BrowseQueryConfiguration config = context.BrowseQueryConfigurations.First();
            context.Entry(config).Reference(c => c.SelectedLanguage).Load();
            context.Entry(config).Reference(c => c.SelectedType).Load();
            context.Entry(config).Collection(c => c.Tags).Load();
            return Ok(config.ToDTO());
        }

        [HttpPatch("browse/tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult UpdateBrowseTags(int id, [FromBody] IEnumerable<int> tagIds) {
            BrowseQueryConfiguration? config = context.BrowseQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Collection(c => c.Tags).Load();
            config.Tags.Clear();
            foreach (int tagId in tagIds) {
                Tag? tag = context.Tags.Find(tagId);
                if (tag != null) {
                    config.Tags.Add(tag);
                }
            }
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("browse/language")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateBrowseLanguage(int id, int languageId) {
            BrowseQueryConfiguration? config = context.BrowseQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            GalleryLanguage? language = context.GalleryLanguages.Find(languageId);
            if (language == null) {
                return NotFound();
            }
            config.SelectedLanguage = language;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("browse/type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateBrowseType(int id, int typeId) {
            BrowseQueryConfiguration? config = context.BrowseQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            GalleryType? type = context.GalleryTypes.Find(typeId);
            if (type == null) {
                return NotFound();
            }
            config.SelectedType = type;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("browse/SearchKeywordText")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateBrowseSearchKeywordText(int id, string searchKeywordText) {
            BrowseQueryConfiguration? config = context.BrowseQueryConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            config.SearchKeywordText = searchKeywordText;
            context.SaveChanges();
            return Ok();
        }

    }
}
