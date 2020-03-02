using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.Currency.Dtos;
using Doppler.Currency.Logger;

namespace Doppler.Currency.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ILoggerAdapter<CurrencyService> _logger;
        private readonly IReadOnlyDictionary<CurrencyType, CurrencyHandler> _currencyHandlers;

        public CurrencyService(
            ILoggerAdapter<CurrencyService> logger,
            IReadOnlyDictionary<CurrencyType, CurrencyHandler> currencyHandlers) =>
            (_logger, _currencyHandlers) = (logger, currencyHandlers);

        public async Task<EntityOperationResult<UsdCurrency>> GetUsdCurrencyByCountryAndDate(DateTime date, string countryCode)
        {
            try
            {
                _logger.LogInformation("Service Getting currency type.");
                Enum.TryParse(typeof(CurrencyType), countryCode, true, out var result);
                _currencyHandlers.TryGetValue((CurrencyType) result, out var handler);

                if (handler != null)
                    return await handler.Handle(date);
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Invalid country code.");
                var result2 = new EntityOperationResult<UsdCurrency>();
                result2.AddError("Country code invalid", $"Currency country invalid: {countryCode}");
                return result2;
            }

            return null;
        }
    }
}
