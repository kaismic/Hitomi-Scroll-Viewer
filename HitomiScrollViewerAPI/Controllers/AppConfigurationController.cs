﻿using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/app-config")]
    public class AppConfigurationController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<AppConfigurationDTO> GetConfiguration() {
            AppConfiguration config = context.AppConfigurations.First();
            return Ok(config.ToDTO());
        }

        [HttpPatch("is-first-launch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateIsFirstLaunch(int configId, [FromBody] bool value) {
            AppConfiguration? config = context.AppConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.IsFirstLaunch = value;
            context.SaveChanges();
            return Ok();
        }
        
        [HttpPatch("app-language")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateAppLanguage(int configId, [FromBody] string value) {
            AppConfiguration? config = context.AppConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.AppLanguage = value;
            context.SaveChanges();
            return Ok();
        }
        
        [HttpPatch("last-update-check-time")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateLastUpdateCheckTime(int configId, [FromBody] DateTimeOffset value) {
            AppConfiguration? config = context.AppConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.LastUpdateCheckTime = value;
            context.SaveChanges();
            return Ok();
        }
    }
}
