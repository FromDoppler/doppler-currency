using System;
using Doppler.Currency.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Doppler.Currency.Test.Helper
{
    public static class HelperExtension
    {
        public static void VerifyLogger(this Mock<ILogger<CurrencyHandler>> logger, LogLevel logLevel, string textCheck, Times times)
        {
            logger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Equals(textCheck)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                times);
        }
    }
}