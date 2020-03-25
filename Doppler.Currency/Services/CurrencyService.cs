using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;
using Microsoft.Extensions.Logging;

namespace Doppler.Currency.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ILogger<CurrencyService> _logger;
        private readonly IReadOnlyDictionary<CurrencyCodeEnum, CurrencyHandler> _currencyHandlers;

        public CurrencyService(
            ILogger<CurrencyService> logger,
            IReadOnlyDictionary<CurrencyCodeEnum, CurrencyHandler> currencyHandlers) =>
            (_logger, _currencyHandlers) = (logger, currencyHandlers);

        public async Task<EntityOperationResult<CurrencyDto>> GetCurrencyByCurrencyCodeAndDate(
            DateTime date,
            CurrencyCodeEnum currencyCode)
        {
            var result = new EntityOperationResult<CurrencyDto>();
            try
            {
                _logger.LogInformation("Service Getting currency code handler.");
                _currencyHandlers.TryGetValue(currencyCode, out var handler);

                if (handler != null)
                    return await handler.Handle(date);
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Error to get currency.");
                result.AddError("Error to get currency", $"Please see log, currency code : {currencyCode} and date  {date}.");
                return result;
            }

            result.AddError("Currency code invalid", $"Currency code invalid: {currencyCode}.");
            return result;
        }
    }
}
