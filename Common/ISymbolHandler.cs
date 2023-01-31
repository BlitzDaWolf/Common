using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;

namespace Common
{
    public struct SymbolDate
    {
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal low { get; set; }
        public decimal high { get; set; }
        public decimal volume { get; set; }
        public DateTime time { get; set; }
    }

    public interface ISymbolHandler
    {
        long SymbolId { get; }

        Dictionary<string, List<OLHC>> Bars { get; }
        Dictionary<string, OLHC> CurrentBar { get; }
        Dictionary<string, IStrategy> Strategies { get; }

        void OnTrend(SymbolDate data, string timeFrame)
        {
            if (Bars.ContainsKey(timeFrame))
            {
                if (CurrentBar[timeFrame].Volume > data.volume)
                {
                    OLHC q = new OLHC()
                    {
                        Close =     data.close,
                        Low =       data.low,
                        High =      data.high,
                        Volume =    data.volume,
                        Open =      data.open,
                        Date =      data.time,
                        SymbolId = SymbolId,
                    };
                    Bars[timeFrame].Add(CurrentBar[timeFrame]);
                    CurrentBar[timeFrame] = q;
                    if (Strategies.ContainsKey(timeFrame))
                    {
                        Strategies[timeFrame].OnBar(Bars[timeFrame]);
                    }
                }

                CurrentBar[timeFrame] = new OLHC()
                {
                    Close =     data.close,
                    Low =       data.low,
                    High =      data.high,
                    Volume =    data.volume,
                    Open =      data.open,
                    Date =      data.time,
                    SymbolId = SymbolId,
                };
            }
        }

        void OnTrends(SymbolDate[] data, string timeFrame)
        {
            if (!Bars.ContainsKey(timeFrame))
            {
                Bars.Add(timeFrame, new List<OLHC>());
                for (int i = 0; i < data.Length - 1; i++)
                {
                    var current = data[i];
                    Bars[timeFrame].Add(new OLHC()
                    {
                        Close =     current.close,
                        Low =       current.low,
                        High =      current.high,
                        Volume =    current.volume,
                        Open =      current.open,
                        Date =      current.time,
                        SymbolId = SymbolId,
                    });
                }
                {
                    var current = data.Last();
                    CurrentBar.Add(timeFrame, new OLHC()
                    {
                        Close =     current.close,
                        Low =       current.low,
                        High =      current.high,
                        Volume =    0, //current.volume,
                        Open =      current.open,
                        Date =      current.time,
                        SymbolId = SymbolId,
                    });
                }
            }
        }

        void AddStrategy(IStrategy strategy, string timeframe)
        {
            if(!Strategies.ContainsKey(timeframe))
            {
                Strategies.Add(timeframe, strategy);
            }
        }
    }
}
