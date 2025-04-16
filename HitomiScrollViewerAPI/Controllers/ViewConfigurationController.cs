using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/view-config")]
    public class ViewConfigurationController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ViewConfigurationDTO> GetConfiguration() {
            ViewConfiguration config = context.ViewConfigurations.First();
            return Ok(config.ToDTO());
        }
    }
}
