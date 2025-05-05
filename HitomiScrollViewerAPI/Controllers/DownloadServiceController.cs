using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerData;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/download-service")]
    public class DownloadServiceController(IEventBus<DownloadEventArgs> eventBus) : ControllerBase {
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateDownloader([FromBody] int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Create,
                GalleryId = galleryId,
            });
            return Ok();
        }

        [HttpPost("start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult StartDownloader([FromBody] int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Start,
                GalleryId = galleryId,
            });
            return Ok();
        }

        [HttpPost("pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult PauseDownloader([FromBody] int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Pause,
                GalleryId = galleryId,
            });
            return Ok();
        }

        [HttpPost("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult DeleteDownloader([FromBody] int galleryId) {
            eventBus.Publish(new() {
                Action = DownloadAction.Delete,
                GalleryId = galleryId,
            });
            return Ok();
        }
    }
}
