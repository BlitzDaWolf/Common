using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    public class TradeEnv
    {
        public QTrader trader;
        private List<double> prices;

        public double currentBalance = 10000;
        double volume = 0;
        double startPrice = 0;

        double lastEq;

        int profitTrades = 0;
        int lossTrades = 0;

        public int TotalTrades => profitTrades + lossTrades;
        double score => ((double)(profitTrades) / (double)(profitTrades + lossTrades)) + (currentBalance / 10000);

        public int size = 10;

        public TradeEnv(List<double> prices, double learningRate = 0.01, int size = 10)
        {
            trader = new QTrader(0.05, 0, 10000);
            this.prices = prices;
            lastEq = currentBalance;
            this.size = size;
        }

        public void TrainTick(int index)
        {
            var c = prices[index];
            trader.Balance = currentBalance;
            trader.score = score;
            double profit = GetProfit(c);
            trader.Balance += profit;

            trader.Balance = lastEq;
            int action = -1;
            double[] d = prices.Skip(index - size).Take(size).ToArray();
            var start = d[0];
            // d = d.Select(x => x / d[0]/*Math.Round(x / d[0], 8)*/).ToArray();
            d = d.Select(x => x / start/*Math.Round(x / start, 8)*/).ToArray();

            var tmp = prices.Skip(index).Take(5).ToList();
            var high = tmp.Max();
            var low = tmp.Min();
            var idxHigh = tmp.IndexOf(high);
            var idxLow = tmp.IndexOf(low);

            var tHigh = Math.Abs(c / high - 1);
            var tLow = Math.Abs(c / low - 1);

            trader.Train(d, (tHigh > tLow) ? 0 : 1);
            lastEq = currentBalance + profit;
            double currentState = trader.GetState(d.Sum());
            action = trader.GetAction(currentState);

            if (action == 0)
            {
                Buy(c);
            }
            else if (action == 1)
            {
                Sell(c);
            }
            else if (action == 2)
            {
                Close(c);
            }
        }
        public void Tick(int index)
        {
            if(trader.qTable.Count == 0)
            {
                return;
            }
            var c = prices[index];
            trader.Balance = currentBalance;
            trader.score = score;
            double profit = GetProfit(c);
            trader.Balance += profit;

            trader.Balance = lastEq;
            int action = -1;
            double[] d = prices.Skip(index - size).Take(size).ToArray();
            var start = d[0];
            // d = d.Select(x => x / d[0]/*Math.Round(x / d[0], 8)*/).ToArray();
            d = d.Select(x => x / start/*Math.Round(x / start, 8)*/).ToArray();

            var tmp = prices.Skip(index).Take(25).ToList();
            var high = tmp.Max();
            var low = tmp.Min();
            var idxHigh = tmp.IndexOf(high);
            var idxLow = tmp.IndexOf(low);

            var tHigh = Math.Abs(c / high - 1);
            var tLow = Math.Abs(c / low - 1);

            double currentState = trader.GetState(d.Sum());
            action = trader.GetAction(currentState);
            lastEq = currentBalance + profit;

            if (action == 0)
            {
                Buy(c);
            }
            else if (action == 1)
            {
                Sell(c);
            }
            else if (action == 2)
            {
                Close(c);
            }
        }

        private double GetProfit(double current)
        {
            double diffrence = current - startPrice;
            return diffrence * (100000 * volume);
        }

        public void Close(double current)
        {
            if(volume == 0)
            {
                return;
            }
            double diffrence = current - startPrice;
            double profit = diffrence * (100000 * volume);
            if (profit < 0)
            {
                lossTrades++;
            }
            else
            {
                profitTrades++;
            }
            currentBalance += (100000 * Math.Abs(volume)) + profit;
            volume = 0;
            startPrice = 0;
            currentBalance -= (6 * 0.01);
        }

        private void Sell(double current)
        {
            if(volume > 0)
            {
                Close(current);
            }
            if (volume != 0)
            {
                return;
            }
            currentBalance -= (100000 * 0.01);
            volume = -0.01;
            startPrice = current;
        }

        private void Buy(double current)
        {
            if (volume < 0)
            {
                Close(current);
            }
            if(volume != 0)
            {
                return;
            }
            currentBalance -= (100000 * 0.01);
            volume = 0.01;
            startPrice = current;
        }

        internal void Reset()
        {
            currentBalance = 10000;
            lossTrades = 0;
            profitTrades = 0;
        }
    }
}
