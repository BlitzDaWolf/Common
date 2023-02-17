using Common;
using Newtonsoft.Json.Linq;
using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPICtrader.Interface;
using System.Threading;

namespace TestApplication.NewFolder
{
    internal class Handler : IHandeler
    {
        public Type SymbolHandlerType { get; }
        public Dictionary<long, ISymbolHandler> Symbols { get; }

        public Handler()
        {
            SymbolHandlerType = typeof(SymbolHandler);
            Symbols = new Dictionary<long, ISymbolHandler>();
        }

        public async void AccountAuthRequest(Token token, OpenClient client, long accountId)
        {
            var request = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = accountId,
                AccessToken = token.AccessToken
            };

            await client.SendMessage(request);
        }

        public async void GetClients(Token token, OpenClient client)
        {
            var request = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = token.AccessToken,
            };

            await client.SendMessage(request);
        }

        public long OnAuth(ProtoOAGetAccountListByAccessTokenRes message)
        {
            return (long)message.CtidTraderAccount.First().CtidTraderAccountId;
        }

        public void OnExecutionEvent(ProtoOAExecutionEvent message)
        {

        }

        public void OnTick(ProtoOASpotEvent message)
        {
            if (message.Trendbar.Count != 0)
            {
                OnTrend(message);
            }
        }

        public void OnTrend(ProtoOASpotEvent message)
        {
            var trend = message.Trendbar[0];
            var Low = trend.Low;
            var open = Low + (long)trend.DeltaOpen;
            var High = Low + (long)trend.DeltaHigh;
            var close = message.Bid / 100000m;

            SymbolDate symbolDate = new SymbolDate
            {
                open = open / 100000m,
                close = close,
                low = Low / 100000m,
                high = High / 100000m,
                volume = trend.Volume,
                time = DateTime.Now
            };

            // Logger.LogInformation(Newtonsoft.Json.JsonConvert.SerializeObject(symbolDate));
            try
            {
                Symbols[message.SymbolId].OnTrend(symbolDate, trend.Period.ToString());
            }
            catch (Exception ex)
            {

            }
        }

        public void OnTrends(ProtoOAGetTrendbarsRes message)
        {
            SymbolDate[] data = new SymbolDate[message.Trendbar.Count];
            for (int i = 0; i < data.Length; i++)
            {
                var trend = message.Trendbar[i];
                var Low = trend.Low;
                var open = Low + (long)trend.DeltaOpen;
                var High = Low + (long)trend.DeltaHigh;
                var close = Low + (long)trend.DeltaClose;

                SymbolDate symbolDate = new SymbolDate
                {
                    open = open / 100000m,
                    close = close / 100000m,
                    low = Low / 100000m,
                    high = High / 100000m,
                    volume = trend.Volume,
                    time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(trend.UtcTimestampInMinutes)
                };

                data[i] = symbolDate;
            }
            try
            {
                Console.WriteLine("Saving result");
                File.WriteAllText("./result.json", Newtonsoft.Json.JsonConvert.SerializeObject(data));
                //Symbols[message.SymbolId].OnTrends(data, message.Period.ToString());
            }
            catch (Exception ex)
            {

            }
        }

        public async void Subscribe(Token token, OpenClient client, ProtoOATrendbarPeriod timeframe, long symbolId, long traderId)
        {
            if (!Symbols.ContainsKey(symbolId))
            {
                Symbols.Add(symbolId, (ISymbolHandler)Activator.CreateInstance(SymbolHandlerType, symbolId));
            }
            GetTrends(token, client, timeframe, symbolId, traderId);
            {
                var request = new ProtoOASubscribeSpotsReq()
                {
                    CtidTraderAccountId = traderId,
                };

                request.SymbolId.Add(symbolId);

                await client.SendMessage(request);
            }
            {
                var request = new ProtoOASubscribeLiveTrendbarReq()
                {
                    Period = timeframe,
                    CtidTraderAccountId = traderId,
                    SymbolId = symbolId,
                };

                await client.SendMessage(request);
            }
        }

        public void Subscribe(Token token, OpenClient client, ProtoOATrendbarPeriod timeframe, long symbolId, long traderId, IStrategy strategy)
        {
            if (!Symbols.ContainsKey(symbolId))
            {
                Symbols.Add(symbolId, (ISymbolHandler)Activator.CreateInstance(SymbolHandlerType, symbolId));
            }
            Subscribe(token, client, timeframe, symbolId, traderId);
            Symbols[symbolId].Strategies.Add(timeframe.ToString(), strategy);
        }

        async void GetTrends(Token token, OpenClient client, ProtoOATrendbarPeriod timeframe, long symbolId, long traderId)
        {
            var request = new ProtoOAGetTrendbarsReq
            {
                CtidTraderAccountId = traderId,
                SymbolId = symbolId,
                Period = timeframe,
                FromTimestamp = DateTimeOffset.UtcNow.AddDays(-4).ToUnixTimeMilliseconds(),
                ToTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            await client.SendMessage(request);
        }

        public void GetPositions(OpenClient client, long traderId)
        {
            throw new NotImplementedException();
        }

        public void OnExecutionEvent(ProtoOAExecutionEvent message, IOpenClient client)
        {
            throw new NotImplementedException();
        }
    }
}
