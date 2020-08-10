using System;
using System.Threading.Tasks;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;
using Doppler.Currency.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Doppler.Currency.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ILogger<CurrencyController> _logger;
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ILogger<CurrencyController> logger, ICurrencyService currencyService) => 
            (_logger, _currencyService) = (logger, currencyService);

        [HttpGet("{currencyCode}/{date}")]
        [SwaggerOperation(Summary = "Get currency by currency code and date")]
        [SwaggerResponse(200, "The currency is ok", typeof(CurrencyDto))]
        [SwaggerResponse(400, "The currency data is invalid")]
        public async Task<IActionResult> Get(
            [SwaggerParameter(Description = "yyyy-MM-dd")] DateTime date,
            [SwaggerParameter(Description = "ARS=1, MXN=2")] CurrencyCodeEnum currencyCode)
        {
            _logger.LogInformation("Parsing dateTime");

            if (date.Year == 1 || date.Date > DateTime.Now)
            {
                return BadRequest($"Invalid Date {date}");
            }

            _logger.LogInformation("Getting Usd currency");
            var result = await _currencyService.GetCurrencyByCurrencyCodeAndDate(date, currencyCode);

            if (result.Success)
                return Ok(result.Entity);

            return BadRequest(result);
        }
    }
}
