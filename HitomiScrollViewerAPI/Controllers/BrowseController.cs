﻿using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class BrowseController(HitomiContext context) : ControllerBase {

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<BrowseConfigurationDTO> GetConfiguration() {
            BrowseConfiguration config = context.BrowseConfigurations.First();
            context.Entry(config).Reference(c => c.SelectedLanguage).Load();
            context.Entry(config).Reference(c => c.SelectedType).Load();
            context.Entry(config).Collection(c => c.Tags).Load();
            return Ok(config.ToDTO());
        }

        [HttpPatch("tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult UpdateBrowseTags(int id, [FromBody] IEnumerable<int> tagIds) {
            BrowseConfiguration? config = context.BrowseConfigurations.Find(id);
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

        [HttpPatch("language")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateBrowseLanguage(int id, int languageId) {
            BrowseConfiguration? config = context.BrowseConfigurations.Find(id);
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
        public ActionResult UpdateBrowseType(int id, int typeId) {
            BrowseConfiguration? config = context.BrowseConfigurations.Find(id);
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

        [HttpPatch("SearchKeywordText")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateBrowseSearchKeywordText(int id, string searchKeywordText) {
            BrowseConfiguration? config = context.BrowseConfigurations.Find(id);
            if (config == null) {
                return NotFound();
            }
            config.SearchKeywordText = searchKeywordText;
            context.SaveChanges();
            return Ok();
        }

    }
}
