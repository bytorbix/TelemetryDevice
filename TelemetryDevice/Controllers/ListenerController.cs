using Microsoft.AspNetCore.Mvc;
using TelemetryDevice.ListenerBlock;

namespace TelemetryDevice.Controllers
{
    public record StartListenerRequest(string Ip, int Port);

    [ApiController]
    [Route("api/listener")]
    public class ListenerController(Listener listener) : ControllerBase
    {
        [HttpPost("start")]
        public IActionResult Start([FromBody] StartListenerRequest request)
        {
            try
            {
                listener.StartListening(request.Ip, request.Port);
                return Accepted();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            try
            {
                listener.StopListening();
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
