using Common;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Strategys
{
    public class HighLow : BaseStrategy
    {
        bool canShort = false;

        public override void OnBar(List<OLHC> bars)
        {
            var cmf = bars.GetCmf(208).Last();
            var Alligator = bars.GetAlligator(105, 79, 161, 210, 219, 102).Last();
            var superTrend = bars.GetSuperTrend(20, 3).Last();
            var aroon = bars.GetAroon(71).Last();

            Console.WriteLine("Got bar");

            if (cmf.Cmf > 0 && Alligator.Lips > Alligator.Teeth && Alligator.Teeth > Alligator.Jaw)
            {
                if (aroon.AroonUp > 70)
                {
                    if (superTrend.UpperBand < bars.Last().Low)
                    {
                        // Long
                        Client.Buy(1, 100, StopLoss: superTrend.UpperBand.Value);
                    }
                }
            }
            else if (cmf.Cmf < 0 && Alligator.Lips < Alligator.Teeth && Alligator.Teeth < Alligator.Jaw)
            {
                if (aroon.AroonDown > 70)
                {
                    if (superTrend.LowerBand > bars.Last().High)
                    {
                        // Short
                        if (canShort)
                        {
                            Client.Sell(1, 100, StopLoss: superTrend.LowerBand.Value);
                        }
                    }
                }
            }

            /*Console.WriteLine(bars.Last().Close);
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(superTrend));


            Console.WriteLine($"{adx.Adx} > 25 && {cmf.Cmf} > 0 && {vortex.Nvi} < 0 && {Alligator.Lips} > {Alligator.Teeth} && {Alligator.Teeth} > {Alligator.Jaw}");
            Console.WriteLine($"{adx.Adx} < 20 && {cmf.Cmf} < 0 && {vortex.Nvi} > 0 && {Alligator.Lips} < {Alligator.Teeth} && {Alligator.Teeth} < {Alligator.Jaw}");*/

            /*Console.WriteLine($"{adx.Adx > 25} && {cmf.Cmf > 0} && {vortex.Nvi < 0} && {Alligator.Lips > Alligator.Teeth} && {Alligator.Teeth > Alligator.Jaw}");
            Console.WriteLine($"{adx.Adx < 20} && {cmf.Cmf < 0} && {vortex.Nvi > 0} && {Alligator.Lips < Alligator.Teeth} && {Alligator.Teeth < Alligator.Jaw}");

            try
            {
                float _long = adx.Adx > 25 ? 1 : 0;
                _long += cmf.Cmf > 0 ? 1 : 0;
                _long += vortex.Nvi < 0 ? 1 : 0;
                _long += Alligator.Lips > Alligator.Teeth ? 1 : 0;
                _long += Alligator.Teeth > Alligator.Jaw ? 1 : 0;
                float _short = adx.Adx < 20 ? 1 : 0;
                _short += cmf.Cmf < 0 ? 1 : 0;
                _short += vortex.Nvi > 0 ? 1 : 0;
                _short += Alligator.Lips < Alligator.Teeth ? 1 : 0;
                _short += Alligator.Teeth < Alligator.Jaw ? 1 : 0;

                // 1 + 1 + 1 + 1 + 1;

                Console.WriteLine(_long);
                Console.WriteLine(_short);
                Console.WriteLine();
                Console.WriteLine(_long / 5f);
                Console.WriteLine(_short / 5f);
                Console.WriteLine();

            }
            catch
            {

            }
            if (adx.Adx > 25 && cmf.Cmf > 0 && vortex.Nvi < 0 && Alligator.Lips > Alligator.Teeth && Alligator.Teeth > Alligator.Jaw)
            {
                if (superTrend.LowerBand != null)
                {
                    Client.Buy(1, 25, StopLoss: superTrend.LowerBand.Value);
                }
            }
            else if (adx.Adx < 20 && cmf.Cmf < 0 && vortex.Nvi > 0 && Alligator.Lips < Alligator.Teeth && Alligator.Teeth < Alligator.Jaw)
            {
                if (superTrend.UpperBand != null)
                {
                    Client.Sell(1, 25, StopLoss: superTrend.UpperBand.Value);
                }
            }
            else
            {
                // exit any existing positions
            }*/
        }
    }
}
