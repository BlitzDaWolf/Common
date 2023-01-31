using Common;
using Skender.Stock.Indicators;
using System.Collections.Generic;

namespace Strategys
{
    public abstract class BaseStrategy : IStrategy
    {
        public IClient Client { get; set; }

        public void Dispose() { }
        public void Init() { }
        public virtual void OnBar(List<OLHC> bars) { }
        public virtual void OnTick(List<OLHC> bars) { }
    }
}
