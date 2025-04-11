﻿using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/search-config")]
    public class SearchConfigurationController(HitomiContext context) : ControllerBase {
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

        [HttpPatch("tag-filter-collection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateIncludeTagFilters(int configId, bool isInclude, [FromBody] IEnumerable<int> tagFilterIds) {
            SearchConfiguration? config = context.SearchConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            if (isInclude) {
                config.SelectedIncludeTagFilterIds = tagFilterIds;
            } else {
                config.SelectedExcludeTagFilterIds = tagFilterIds;
            }
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
    }
}
