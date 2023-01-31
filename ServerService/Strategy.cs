using Common;
using Microsoft.Extensions.Logging;
using OpenAPICtrader.Interface;
using Skender.Stock.Indicators;
using Strategys;
using System.Numerics;

namespace ServerService
{
    public class Strategy : BaseStrategy
    {
        private ILogger<Strategy> _logger;

        public Strategy(IOpenClient clinet, ILogger<Strategy> logger)
        {
            Client = clinet;
            clinet.Subscribe(1, "M1", this);
            // clinet.Subscribe(2, "M1", this);
            _logger = logger;
        }

        public override void OnBar(List<OLHC> bars)
        {
            OLHC current = bars.Last();

            var ema = bars.GetEma(50).Last();
            var aroon = bars.GetAroon(20).Last();
            var trend = bars.GetSuperTrend().Last();

            if (trend.UpperBand != null)
            {
                _logger.LogInformation("Upper");
                if (aroon.AroonUp > 70 && aroon.AroonDown < 50)
                {
                    // trend.UpperBand
                    //Client.Sell(current.SymbolId, 100000, StopLoss: 50);
                }
            }
            if (trend.LowerBand != null)
            {
                _logger.LogInformation("Lower");
                if (aroon.AroonDown > 70 && aroon.AroonUp < 50)
                {
                    // trend.UpperBand
                    //Client.Buy(current.SymbolId, 100000, StopLoss: 50);
                }
            }
        }
    }
}
