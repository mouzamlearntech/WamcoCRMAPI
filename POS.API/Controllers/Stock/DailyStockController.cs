using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POS.API.Helpers;
using POS.Data.Dto;
using POS.Data.Resources;
using POS.MediatR;

namespace POS.API.Controllers.Stock
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DailyStockController : BaseController
    {
        public IMediator _mediator { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DailyStockController"/> class.
        /// </summary>
        /// <param name="mediator">The mediator.</param>
        public DailyStockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all daily stocks.
        /// </summary>
        /// <returns>A list of daily stock items.</returns>
        [HttpGet]
        [ClaimCheck("REP_DAILY_STOCK_REP")]
        [Produces("application/json", "application/xml", Type = typeof(List<DailyStockDto>))]
        public async Task<IActionResult> GetAllDailyStocks([FromQuery] DailyStockResource dailyStockResource)
        {
            var getAllDailyStockQuery = new GetAllDailyStockQuery
            {
                DailyStockResource = dailyStockResource
            };
            var dailyStockDtos = await _mediator.Send(getAllDailyStockQuery);

            var paginationMetadata = new
            {
                totalCount = dailyStockDtos.TotalCount,
                pageSize = dailyStockDtos.PageSize,
                skip = dailyStockDtos.Skip,
                totalPages = dailyStockDtos.TotalPages
            };

            Response.Headers.Append("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            return Ok(dailyStockDtos);
        }

    }
}
