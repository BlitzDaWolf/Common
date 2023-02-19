using OpenAPICtrader.Interface;

namespace ServerService
{
    public class StrategySymbol
    {
        public long SymbolID { get; set; }
        public string TimeFrame { get; set; }
        public string StrategyName { get; set; }
        public decimal TPPip { get; set; }
        public decimal SLPip { get; set; }
        public long LotSize { get; set; }
        public decimal Pipsize { get; set; }
    }

    public class StrategyDispatcher
    {
        public StrategyDispatcher(IOpenClient clinet, ILogger<Strategy> logger, IConfiguration config)
        {
            var l = config.GetSection("strategy:value").Get<StrategySymbol[]>();
            // foreach (var item in l)
            for (int i = 0; i < l.Length; i++)
            {
                var item = l[i];
                var strat = new Strategy(clinet, item);
                clinet.Subscribe(item.SymbolID, item.TimeFrame, strat, i);
            }
        }
    }
}
