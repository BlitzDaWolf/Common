using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;

namespace Common
{
    public interface IStrategy : IDisposable
    {
        IClient Client { get; set; }

        void Init();

        void OnBar(List<OLHC> bars);
    }
}
