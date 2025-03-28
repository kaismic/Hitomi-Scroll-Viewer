using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/search")]
    public class SearchController(HitomiContext context) : ControllerBase {
        [HttpGet("debug")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DebugAPI() {
            //Console.WriteLine(context.SearchFilters.Count());
            //context.SearchFilters.Load();
            //context.SearchFilters.RemoveRange(context.SearchFilters);
            //context.SaveChanges();
            Console.WriteLine(context.SearchFilters.Count());
            Console.WriteLine(context.LabeledTagCollections.Count());
            return Ok();
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SearchConfigurationDTO> GetConfiguration() {
            SearchConfiguration config =
                context.SearchConfigurations
                .Include(c => c.SelectedLanguage)
                .Include(c => c.SelectedType)
                .Include(c => c.TagFilters)
                .Include(c => c.SearchFilters)
                .ThenInclude(sf => sf.LabeledTagCollections)
                .First();
            return Ok(config.ToDTO());
        }

        [HttpPatch("enable-auto-save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateAutoSave(int configId, bool enable) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            if (config.IsAutoSaveEnabled == enable) {
                return Ok();
            }
            config.IsAutoSaveEnabled = enable;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("include-tag-filters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateIncludeTagFilters(int configId, [FromBody] IEnumerable<int> tagFilterIds) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.SelectedIncludeTagFilterIds = tagFilterIds;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("exclude-tag-filters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateExcludeTagFilters(int configId, [FromBody] IEnumerable<int> tagFilterIds) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.SelectedExcludeTagFilterIds = tagFilterIds;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("selected-tag-filter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSelectedTagFilter(int configId, int tagFilterId) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            if (config.SelectedTagFilterId == tagFilterId) {
                return Ok();
            }
            config.SelectedTagFilterId = tagFilterId;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("search-keyword-text")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateSearchKeywordText(int configId, [FromBody] string searchKeywordText) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.SearchKeywordText = searchKeywordText;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("language")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateLanguage(int configId, int languageId) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
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

        [HttpPatch("type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateType(int configId, int typeId) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
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

        [HttpGet("tag-filter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TagFilterDTO> GetTagFilter(int configId, int tagFilterId) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            
            context.Entry(config).Collection(c => c.TagFilters).Load();
            TagFilter? tagFilter = config.TagFilters.Find(tf => tf.Id == tagFilterId);
            if (tagFilter == null) {
                return NotFound();
            }
            return Ok(tagFilter.ToDTO());
        }

        [HttpPost("tag-filter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<int> CreateTagFilter([FromBody] TagFilterBuildDTO dto) {
            SearchConfiguration? config = context.SearchConfigurations.Find(dto.SearchConfigurationId);
            if (config == null) {
                return NotFound();
            }
            List<Tag> tags = [.. dto.Tags.Select(t => t.ToEntity())];
            TagFilter tagFilter = new() { Name = dto.Name, Tags = tags };
            context.Tags.AttachRange(tags);
            config.TagFilters.Add(tagFilter);
            context.SaveChanges();
            return Ok(tagFilter.Id);
        }

        [HttpGet("tag-filter/all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<TagFilterDTO>> GetAllTagFilters(int configId) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Collection(c => c.TagFilters).Load();
            return Ok(config.TagFilters.Select(tf => tf.ToDTO()));
        }

        [HttpPost("tag-filter/delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TagFilterDTO> DeleteTagFilters(int configId, [FromBody] IEnumerable<int> tagFilterIds) {
            context.TagFilters.RemoveRange(context.TagFilters.Where(tf => tagFilterIds.Contains(tf.Id)));
            // should use below when there are multiple configs (users)
            //context.TagFilters
            //    .Include(tf => tf.SearchConfiguration)
            //    .Where(tf => tagFilterIds.Contains(tf.Id) && tf.SearchConfiguration.Id == configId);
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("tag-filter/name")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateTagFilterName(int configId, int tagFilterId, [FromBody] string name) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Collection(c => c.TagFilters).Load();
            TagFilter? tagFilter = config.TagFilters.Find(tf => tf.Id == tagFilterId);
            if (tagFilter == null) {
                return NotFound();
            }
            tagFilter.Name = name;
            context.SaveChanges();
            return Ok();
        }

        [HttpGet("tag-filter/tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<TagDTO>> GetTagFilterTags(int configId, int tagFilterId) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Collection(c => c.TagFilters).Load();
            TagFilter? tagFilter = config.TagFilters.Find(tf => tf.Id == tagFilterId);
            if (tagFilter == null) {
                return NotFound();
            }
            context.Entry(tagFilter).Collection(tf => tf.Tags).Load();
            return Ok(tagFilter.Tags.Select(tag => tag.ToDTO()));
        }

        [HttpPatch("tag-filter/tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateTagFilterTags(int configId, int tagFilterId, [FromBody] IEnumerable<int> tagIds) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Collection(c => c.TagFilters).Load();
            TagFilter? tagFilter = config.TagFilters.Find(tf => tf.Id == tagFilterId);
            if (tagFilter == null) {
                return NotFound();
            }
            context.Entry(tagFilter).Collection(tf => tf.Tags).Load();
            tagFilter.Tags.Clear();
            foreach (int id in tagIds) {
                Tag? tag = context.Tags.Find(id);
                if (tag != null) {
                    tagFilter.Tags.Add(tag);
                }
            }
            context.SaveChanges();
            return Ok();
        }

        // return union of tags for each tag filters
        [HttpPost("tag-filter/tags-union")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<IEnumerable<TagDTO>> GetTagFilterTagsUnion(int configId, [FromBody] IEnumerable<int> tagFilterIds) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            context.Entry(config).Collection(c => c.TagFilters).Load();
            IEnumerable<TagDTO>? tags = [];
            foreach (int tagFilterId in tagFilterIds) {
                TagFilter? tagFilter = config.TagFilters.Find(tf => tf.Id == tagFilterId);
                if (tagFilter != null) {
                    context.Entry(tagFilter).Collection(tf => tf.Tags).Load();
                    tags = tags.UnionBy(tagFilter.Tags.Select(t => t.ToDTO()), t => t.Id);
                }
            }
            if (!tags.Any()) {
                return NoContent();
            }
            return Ok(tags);
        }

        [HttpPost("search-filter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<int> CreateSearchFilter(int configId, [FromBody] SearchFilterDTO dto) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            GalleryLanguage? l = context.GalleryLanguages.Find(dto.Language.Id);
            if (l == null) {
                return NotFound("Language id is not valid.");
            }
            GalleryType? t = context.GalleryTypes.Find(dto.Type.Id);
            if (t == null) {
                return NotFound("Type id is not valid.");
            }
            SearchFilter sf = dto.ToEntity();
            sf.Language = l;
            sf.Type = t;
            config.SearchFilters.Add(sf);
            context.SaveChanges();
            return Ok(sf.Id);
        }

        [HttpDelete("search-filter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteSearchFilter(int configId, int searchFilterId) {
            SearchFilter? searchFilter = context.SearchFilters
                .Include(sf => sf.SearchConfiguration)
                .FirstOrDefault(sf => sf.Id == searchFilterId && sf.SearchConfiguration.Id == configId);
            if (searchFilter == null) {
                return NotFound();
            }
            context.SearchFilters.Remove(searchFilter);
            context.SaveChanges();
            return Ok();
        }

        [HttpDelete("search-filter/clear")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult ClearSearchFilter(int configId) {
            SearchConfiguration? config = context.SearchConfigurations
                .Include(c => c.SearchFilters)
                .FirstOrDefault(c => c.Id == configId);
            if (config == null) {
                return NotFound();
            }
            context.SearchFilters.RemoveRange(config.SearchFilters);
            context.SaveChanges();
            return Ok();
        }
    }
}
