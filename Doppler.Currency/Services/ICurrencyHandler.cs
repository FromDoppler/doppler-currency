using System;
using System.Threading.Tasks;
using CrossCutting;
using Doppler.Currency.Dtos;

namespace Doppler.Currency.Services
{
    public interface ICurrencyHandler
    {
        Task<EntityOperationResult<UsdCurrency>> Handle(DateTime date);
    }
}
