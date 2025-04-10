using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<DownloadConfiguration> GetConfiguration() {
            DownloadConfiguration config = context.DownloadConfigurations.First();
            context.Entry(config).Collection(c => c.DownloadItems).Load();
            return Ok(config.ToDTO());
        }

        [HttpPatch("enable-parallel-download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateParallelDownload(int configId, bool enable) {
            DownloadConfiguration? config = context.DownloadConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.UseParallelDownload = enable;
            context.SaveChanges();
            return Ok();
        }

        [HttpPatch("update-thread-num")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateThreadNum(int configId, int threadNum) {
            DownloadConfiguration? config = context.DownloadConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.ThreadNum = threadNum;
            context.SaveChanges();
            return Ok();
        }
    }
}
