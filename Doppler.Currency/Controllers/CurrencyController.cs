using System;
using System.Threading.Tasks;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;
using Doppler.Currency.Logger;
using Doppler.Currency.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Doppler.Currency.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ILoggerAdapter<CurrencyController> _logger;
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ILoggerAdapter<CurrencyController> logger, ICurrencyService currencyService) => 
            (_logger, _currencyService) = (logger, currencyService);

        [HttpGet("{currencyCode}/{date}")]
        [SwaggerOperation(Summary = "Get currency by country and date")]
        [SwaggerResponse(200, "The currency is ok", typeof(CurrencyDto))]
        [SwaggerResponse(400, "The currency data is invalid")]
        public async Task<IActionResult> Get(
            [SwaggerParameter(Description = "dd-MM-yyyy")] string date,
            [SwaggerParameter(Description = "ARS, MXN")] CurrencyCodeEnum currencyCode)
        {
            _logger.LogInformation("Parsing dateTime");
            DateTime.TryParse(date, out var dateTime);

            if (dateTime.Year == 1 || dateTime.Date > DateTime.Now)
            {
                return BadRequest($"Invalid Date {date}");
            }

            _logger.LogInformation("Getting Usd currency");
            var result = await _currencyService.GetCurrencyByCurrencyCodeAndDate(dateTime, currencyCode);

            if (result.Success)
                return Ok(result);

            return BadRequest(result.Errors);
        }
    }
}
