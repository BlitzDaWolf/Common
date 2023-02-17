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
    public class StrategySymbol
    {
        public long SymbolID { get; set; }
        public string TimeFrame { get; set; }
        public decimal TPPip { get; set; }
        public decimal SLPip { get; set; }
        public long LotSize { get; set; }
    }

    public class Strategy : BaseStrategy
    {
        int input_size = 15;
        private ILogger<Strategy> _logger;

        //public Network Network { get; }
        Dictionary<long, ActivationNetwork> networks = new Dictionary<long, ActivationNetwork>();
        Dictionary<long, StrategySymbol> StrategyValues = new Dictionary<long, StrategySymbol>();

        public Strategy(IOpenClient clinet, ILogger<Strategy> logger, IConfiguration config)
        {
            Client = clinet;
            var l = config.GetSection("strategy:value").Get<StrategySymbol[]>();
            int i = 1;
            foreach (var item in l)
            {
                StrategyValues.Add(item.SymbolID, item);
                clinet.Subscribe(item.SymbolID, item.TimeFrame, this, i++);
            }

            /*clinet.Subscribe(22396, "M30", this, 3);
            clinet.Subscribe(22398, "M30", this, 3);*/
            // clinet.Subscribe(2, "M1", this);
            _logger = logger;
            //Network = ActivationNetwork.Load(@"D:\dev\Common\TestApplication\bin\Release\net7.0\Checkpoint\Final.ml");
        }

        public override void OnBar(List<OLHC> bars)
        {
            var v = StrategyValues[bars[0].SymbolId];
            var rsl =  (bars.Last().Close * 0.001m) * 100000;
            var b = bars.Select(x => (double)x.Close).TakeLast((60 / 1) * 24 * 30).ToList();
            if (networks.ContainsKey(bars[0].SymbolId))
            {
                var inp = b.TakeLast(input_size).ToList();
                var result = networks[bars[0].SymbolId].Compute(inp.Select(x => x / inp[0]).ToArray());
                var difrence = Math.Abs(result[0] - result[1]);
                // _logger.LogInformation(bars[0].SymbolId + ": " + string.Join(',', result) + "\n" + difrence);
                long stock = 50 * v.LotSize;
                if (result[0] > result[1])
                {
                    if (difrence > 0.15)
                    {
                        Client.Buy(v.SymbolID, stock, StopLoss: (v.SLPip * v.LotSize) * 100000, TakeProfit: (v.TPPip * v.LotSize) * 100000);
                    }
                }
                else
                {
                    if (difrence > 0.15)
                    {
                        Client.Sell(v.SymbolID, stock, StopLoss: (v.SLPip * v.LotSize) * 100000, TakeProfit: (v.TPPip * v.LotSize) * 100000);
                    }
                }
            }
            else
            {
                ActivationNetwork network = new ActivationNetwork(
                    new SigmoidFunction(),
                    input_size,  // number of inputs
                    10, 16, 4, // number of neurons in the hidden layer
                    2); // number of outputs
                networks.Add(bars[0].SymbolId, network);
            }
            BackPropagationLearning teacher = new BackPropagationLearning(networks[bars[0].SymbolId]);
            teacher.LearningRate = 0.05;
            {
                _logger.LogInformation($"{bars[0].SymbolId}: Training started @ {DateTime.Now}");
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

                inputs =   inputs.TakeLast(25).ToList();
                outputs = outputs.TakeLast(25).ToList();
                {
                    int i = 0;
                    // for (int i = 0; i < 250; i++)
                    double error = double.MaxValue;
                    var lastM = 0;
                    while (error > 0.5 && (DateTime.Now.Minute) != 55)
                    {
                        error = teacher.RunEpoch(inputs.ToArray(), outputs.ToArray());
                        i++;
                    }
                    _logger.LogInformation($"{bars[0].SymbolId}: Training done [{error}] [{sw.Elapsed}]");
                }
                sw.Stop();
            }
        }
    }
}
