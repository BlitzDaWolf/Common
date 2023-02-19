using Accord.Neuro;
using Accord.Neuro.Learning;
using Common;
using Microsoft.Extensions.Logging;
using OpenAPICtrader.Interface;
using Skender.Stock.Indicators;
using Strategys;
using System.Diagnostics;
using System.Net.Mail;
using System.Net.Sockets;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ServerService
{
    enum VolitilityType
    {
        Low, Medium, High,
    }

    public class Strategy : BaseStrategy
    {
        int input_size = 15;

        //public Network Network { get; }
        ActivationNetwork network;
        StrategySymbol StrategyValues;

        public Strategy(IOpenClient clinet, StrategySymbol strategySymbol)
        {
            Client = clinet;
            network = new ActivationNetwork(
                new SigmoidFunction(),
                input_size,  // number of inputs
                10, 16, 4, // number of neurons in the hidden layer
                2); // number of outputs
            StrategyValues = strategySymbol;
        }

        VolitilityType calculateType(double value)
        {
            if (value > 40)
                return VolitilityType.High;
            if (value > 20)
                return VolitilityType.Medium;
            return VolitilityType.Low;
        }

        public override void OnBar(List<OLHC> bars)
        {
            var res = bars.GetMfi().Where(x => x.Mfi != null).Select(x => Math.Abs(x.Mfi.Value - 50)).Select(calculateType).ToList();//.Select(x => Math.Abs(x.Mfi.Value - 50)).ToList();
            bars.GetSuperTrend();
            var v = StrategyValues;
            long stock = 50 * v.LotSize;


            return;


            /*Client.Buy(22398, 50, StopLoss: 100000 * v.SLPip * v.Pipsize, TakeProfit: 100000 * v.TPPip * v.Pipsize);
            _logger.LogInformation($"({v.SymbolID}, {stock}, {100000 * v.SLPip * v.Pipsize}, {100000 * v.TPPip * v.Pipsize})");
            return;*/

            var rsl =  (bars.Last().Close * 0.001m) * 100000;
            var b = bars.Select(x => (double)x.Close).TakeLast((60 / 1) * 24 * 30).ToList();
            if (network!= null)
            {
                var inp = b.TakeLast(input_size).ToList();
                var result = network.Compute(inp.Select(x => x / inp[0]).ToArray());
                var difrence = Math.Abs(result[0] - result[1]);

                if (result[0] > result[1])
                {
                    if (difrence > 0.15)
                    {
                        Client.Buy(v.SymbolID, stock, StopLoss: 100000 * v.SLPip * v.Pipsize, TakeProfit: 100000 * v.TPPip * v.Pipsize);
                    }
                }
                else
                {
                    if (difrence > 0.15)
                    {
                        Client.Sell(v.SymbolID, stock, StopLoss: 100000 * v.SLPip * v.Pipsize, TakeProfit: 100000 * v.TPPip * v.Pipsize);
                    }
                }
            }
            BackPropagationLearning teacher = new BackPropagationLearning(network);
            teacher.LearningRate = 0.05;
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                List<double[]> inputs = new List<double[]>();
                List<double[]> outputs = new List<double[]>();
                var a = 0.00001;
                for (int i = 0; i < b.Count - (input_size + 1); i++)
                {
                    var inp = b.Skip(i).Take(input_size).ToList();
                    var o = b.Skip(i + input_size).Take(2).ToList();
                    inputs.Add(inp.Select(x => x / inp[0]).ToArray());
                    var change = (o[1] / o[0] - 1);
                    outputs.Add(new double[2] { change > a ? 1 : 0, change < -a ? 1 : 0 });
                }

                int t = v.TimeFrame.StartsWith("H") ? 60 : int.Parse(v.TimeFrame.Replace("M", ""));

                {
                    int i = 5;
                    // for (int i = 0; i < 250; i++)
                    var lastM = 0;
                    var score = (double)((DateTime.Now.Minute) % t) / t;
                    while (score < 0.9)
                    {
                        double error = double.MaxValue;
                        var inp = inputs.TakeLast(i).ToList();
                        var o  = outputs.TakeLast(i).ToList();
                        while (error > 0.5 && score < 0.9)
                        {
                            score = (double)((DateTime.Now.Minute) % t) / t;
                            error = teacher.RunEpoch(inp.ToArray(), o.ToArray());
                        }
                        i++;
                    }
                }
                sw.Stop();
            }
        }

        public void OnPip(List<OLHC> bars)
        {
            var st = bars.GetSuperTrend();
            var bb = bars.GetBollingerBands();


        }
    }
}
