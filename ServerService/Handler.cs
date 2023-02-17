using Common;
using Newtonsoft.Json.Linq;
using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPICtrader.Interface;
using System.Threading;

namespace ServerService
{
    internal class Handler : IHandeler
    {
        public Type SymbolHandlerType { get; }
        public Dictionary<long, ISymbolHandler> Symbols { get; }

        private ILogger<Handler> Logger;

        public Handler(ILogger<Handler> logger)
        {
            SymbolHandlerType= typeof(SymbolHandler);
            Symbols = new Dictionary<long, ISymbolHandler>();
            Logger = logger;
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
            Logger.LogInformation("Requesting clients");
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

        public void OnExecutionEvent(ProtoOAExecutionEvent message, IOpenClient client)
        {
            // Logger.LogInformation(message.ToString());
            if (message.ExecutionType == ProtoOAExecutionType.OrderAccepted)
            {
                if (message.Position.PositionStatus == ProtoOAPositionStatus.PositionStatusCreated)
                {
                    if (!client.Positions.ContainsKey(message.Position.PositionId))
                    {
                        client.Positions.Add(message.Position.PositionId, message.Position);
                    }
                }
            }
            else if (message.ExecutionType == ProtoOAExecutionType.OrderFilled)
            {
                if (message.Position.PositionStatus == ProtoOAPositionStatus.PositionStatusClosed)
                {
                    if (client.Positions.ContainsKey(message.Position.PositionId))
                    {
                        Logger.LogInformation($"Positions has beed closed: {message.Position.TradeData.SymbolId}");
                        client.Positions.Remove(message.Position.PositionId);
                    }
                }
            }
            else if (message.ExecutionType == ProtoOAExecutionType.OrderCancelled)
            {
                if (message.Position.PositionStatus == ProtoOAPositionStatus.PositionStatusClosed)
                {
                    if (client.Positions.ContainsKey(message.Position.PositionId))
                    {
                        Logger.LogInformation($"Positions has beed closed: {message.Position.TradeData.SymbolId}");
                        client.Positions.Remove(message.Position.PositionId);
                    }
                }
            }
        }

        public void OnTick(ProtoOASpotEvent message)
        {
            if(message.Trendbar.Count!= 0)
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
                Logger.LogWarning(ex, "");
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
                Symbols[message.SymbolId].OnTrends(data, message.Period.ToString());
            }catch (Exception ex)
            {
                Logger.LogWarning(ex, "");
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
                FromTimestamp = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds(),
                ToTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            await client.SendMessage(request);
        }

        public async void GetPositions(OpenClient client, long traderId)
        {
            /*var request = new ProtoOADealListByPositionIdReq { CtidTraderAccountId = traderId };
            await client.SendMessage(request);*/
        }
    }
}
