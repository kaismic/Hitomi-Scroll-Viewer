using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController(HitomiContext context, DownloadService downloadService) : ControllerBase {
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<DownloadItemDTO>> GetAllDownloadItems() {
            return Ok(context.DownloadItems.AsNoTracking().Select(d => d.ToDTO()));
        }

        [HttpGet("pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<DownloadItemDTO>> PauseDownload(string hubConnectionId) {
            downloadService.PauseDownload(hubConnectionId);
            return Ok();
        }

        [HttpGet("remove")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<DownloadItemDTO>> RemoveDownload(string hubConnectionId) {
            downloadService.RemoveDownload(hubConnectionId);
            return Ok();
        }
    }
}
