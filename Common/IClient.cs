using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
    public interface IClient : IDisposable
    {
        Task<bool> Connect();

        List<Quote> GetQuotes(string symbolName, string timeframe = "5m", int Size = 100);
        void Subscribe(long symbolName, string timeframe);
        void Subscribe(long symbolName, string timeframe, IStrategy strategy);

        void Buy(long SymbolId,
            long Volume,
            string? Label = "",
            string? Comment = "",
            decimal StopLoss = 0);
        void Sell(long SymbolId,
            long Volume,
            string? Label = "",
            string? Comment = "",
            decimal StopLoss = 0);
    }
}
