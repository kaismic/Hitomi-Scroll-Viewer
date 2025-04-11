using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/download-config")]
    public class DownloadConfigurationController(HitomiContext context) : ControllerBase {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<DownloadConfiguration> GetConfiguration() {
            DownloadConfiguration config = context.DownloadConfigurations.First();
            return Ok(config.ToDTO());
        }

        [HttpPatch("enable-parallel-download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateParallelDownload(int configId, [FromBody] bool enable) {
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
        public ActionResult UpdateThreadNum(int configId, [FromBody] int threadNum) {
            DownloadConfiguration? config = context.DownloadConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            config.ThreadNum = threadNum;
            context.SaveChanges();
            return Ok();
        }

        [HttpGet("downloads")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<int>> GetDownloads(int configId) {
            DownloadConfiguration? config = context.DownloadConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            return Ok(config.Downloads);
        }


        [HttpPatch("add-downloads")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult AddDownloads(int configId, [FromBody] IEnumerable<int> ids) {
            DownloadConfiguration? config = context.DownloadConfigurations.Find(configId);
            if (config == null) {
                return NotFound();
            }
            ICollection<int> downloads = config.Downloads;
            foreach (int id in ids) {
                if (!downloads.Contains(id)) {
                    downloads.Add(id);
                }
            }
            context.SaveChanges();
            return Ok();
        }
    }
}
