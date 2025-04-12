using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerData;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/download-service")]
    public class DownloadServiceController(IEventBus<DownloadEventArgs> eventBus) : ControllerBase {
        [HttpPost("start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult StartDownload(int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Start,
                GalleryId = galleryId,
            });
            return Ok();
        }

        [HttpPost("pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult PauseDownload(int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Pause,
                GalleryId = galleryId,
            });
            return Ok();
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DeleteDownload(int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Delete,
                GalleryId = galleryId,
            });
            return Ok();
        }
    }
}
