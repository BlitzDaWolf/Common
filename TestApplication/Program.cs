using Common;
using TestApplication;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

SymbolDate[] data = new SymbolDate[1];
var lines = File.ReadAllText("./test.json");
lines = lines.Replace("Open", "open").Replace("Close", "close");
data = Newtonsoft.Json.JsonConvert.DeserializeObject<SymbolDate[]>(lines);
var t = data.Select(x => (double)x.close).ToList();

void Run(int size = 2)
{
    Console.WriteLine($"Runing {size} For 1000000 epochs");
    List<TradeEnv> tradeEnv = Enumerable.Range(1, 1).Select(x => new TradeEnv(t, 0.01, size)).ToList();

    QTrader best = tradeEnv[0].trader;
    bool first = true;

    for (int e = 0; e < 1000000; e++)
    //while (best.GetReward() < 0)
    {
        /*tradeEnv = tradeEnv.OrderBy(x => x.trader.GetReward()).ToList();
        tradeEnv.ForEach(x => x.Reset());
        tradeEnv.ForEach(x => x.trader.Normalize());*/
        tradeEnv.ForEach(x => x = new TradeEnv(t, 0.01, size));
        for (int i = size; i < t.Count - 25 - 1440; i++)
        {
            tradeEnv.ForEach(x => x.TrainTick(i));
        }

        tradeEnv.ForEach(x => x.Close(t.Last()));
        tradeEnv.ForEach(x => x.Reset());
        for (int i = t.Count - 25 - size - 1440; i < t.Count - 25; i++)
        {
            tradeEnv.ForEach(x => x.Tick(i));
        }
        tradeEnv = tradeEnv.OrderBy(x => x.trader.score).ToList();
        var b = tradeEnv.Last();// FirstOrDefault(y => y.trader.GetReward() == tradeEnv.Max(x => x.trader.GetReward()));
        if (b != null)
        {
            var c = Math.Max(b.trader.score, best.score);
            if (c > best.score || first)
            {
                best = b.trader;
                // File.WriteAllText("./best.json", JsonConvert.SerializeObject(b.trader));
                Saver.WriteToBinaryFile($"./{size}.dat", b.trader);
                best = Saver.ReadFromBinaryFile<QTrader>($"./{size}.dat");
                Console.WriteLine(e + "," + b.trader.score);
                first = false;
            }
            else
            {
                b.trader.Reverse(best);
            }
            if (e % 20 == 0)
            {
                // Console.WriteLine(e + "," + b.trader.GetReward());
            }
            best = Saver.ReadFromBinaryFile<QTrader>($"./{size}.dat");
            Console.Title = (e + ": " + b.trader.score + " - " + (best.score - b.trader.score)).ToString();
        }
        // tradeEnv.ForEach(x => x.trader = Saver.ReadFromBinaryFile<QTrader>($"./{size}.dat"));
        // tradeEnv.Skip(10).Take(10).ToList().ForEach(x => x.trader.Mutate());*/
    }
}

/*for (int i = 2; i < 50; i++)
{*/
Run(8);
//}