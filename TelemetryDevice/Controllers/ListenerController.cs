using Microsoft.AspNetCore.Mvc;
using TelemetryDevice.BuilderBlock;
using TelemetryDevice.ListenerBlock;

namespace TelemetryDevice.Controllers
{
    public record StartListenerRequest(string Ip, int Port);

    [ApiController]
    [Route("api/listener")]
    public class ListenerController(Listener listener, Builder builder) : ControllerBase
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

        [HttpPost("{tailNumber:int}/register")]
        public IActionResult AddTailNumber(int tailNumber)
        {
            builder.AddTailNumber(tailNumber);
            return Ok();
        }

        [HttpDelete("{tailNumber:int}/remove")]
        public IActionResult RemoveTailNumber(int tailNumber)
        {
            builder.RemoveTailNumber(tailNumber);
            return Ok();
        }

        [HttpGet("active")]
        public IActionResult GetActiveTailNumbers()
        {
            return Ok(builder.GetActiveTailNumbers());
        }
    }
}
