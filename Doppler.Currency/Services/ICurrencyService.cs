using System;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.Currency.Dtos;
using Doppler.Currency.Enums;

namespace Doppler.Currency.Services
{
    public interface ICurrencyService
    {
        public Task<EntityOperationResult<CurrencyDto>> GetCurrencyByCurrencyCodeAndDate(DateTime date, CurrencyCodeEnum currencyCode);
    }
}
