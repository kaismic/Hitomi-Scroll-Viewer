using HitomiScrollViewerData.DbContexts;
using Microsoft.AspNetCore.Mvc;

namespace HitomiScrollViewerAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController(HitomiContext context) : ControllerBase {
        private const string STATUS_FILE_PATH = "database-status.txt";

        //[HttpGet("isinitialized")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public ActionResult<bool> IsInitialized() {
        //    if (System.IO.File.Exists(STATUS_FILE_PATH)) {
        //        bool isInitialized = bool.Parse(System.IO.File.ReadAllText(STATUS_FILE_PATH));
        //        return Ok(isInitialized);
        //    } else {
        //        System.IO.File.WriteAllText(STATUS_FILE_PATH, false.ToString());
        //        return Ok(false);
        //    }
        //}

        //[HttpGet("startinitialize")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public ActionResult<bool> StartInitialization() {
            
        //}

        // TODO: use websocket to update UI status
    }
}
