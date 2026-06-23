using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Ebooks.Common;
using Quraaa.Application.Features.Ebooks.Queries.GetEbooks;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    public class EbooksController : ApiClientController
    {
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<EbookResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEbooks([FromQuery] GetEbooksQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }
    }
}
