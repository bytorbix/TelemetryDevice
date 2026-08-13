using Microsoft.AspNetCore.Mvc;
using TelemetryDevice.BuilderBlock;
using TelemetryDevice.ListenerBlock;

namespace TelemetryDevice.Controllers
{
    public record StartListenerRequest(string Ip, int Port, int TailNumber);

    [ApiController]
    [Route("api/listener")]
    public class ListenerController(Listener listener, Builder builder) : ControllerBase
    {
        [HttpPost("start")]
        public IActionResult Start([FromBody] StartListenerRequest request)
        {
            try
            {
                builder.SetExpectedTailNumber(request.TailNumber);
                listener.StartListening(request.Ip, request.Port);
                return Accepted();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (SharpPcap.PcapException ex)
            {
                return StatusCode(503, ex.Message);
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
