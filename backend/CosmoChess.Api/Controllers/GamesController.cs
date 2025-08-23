using CosmoChess.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CosmoChess.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class GamesController(IMediator mediator) : ControllerBase
    {

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzePositionDto dto, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new AnalyzePositionCommand(dto.Fen, dto.Depth), cancellationToken);
            return Ok(new { bestMove = result });
        }

    }
    public record AnalyzePositionDto(string Fen, int Depth);

}
