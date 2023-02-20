using Common;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Helpers;
using OpenAPICtrader.Interface;
using Skender.Stock.Indicators;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace ServerService
{
    public class Client : IOpenClient
    {
        public IHandeler handler { get; set; }

        private readonly ILogger<Client> logger;
        private OpenClient _client;
        private Token _token;
        private App _app;
        private long traderId;
        private readonly string appid;
        private readonly string secret;
        private readonly string token;
        private readonly List<IDisposable> _disposables = new();

        public Dictionary<long, ProtoOAPosition> Positions { get; set; } = new Dictionary<long, ProtoOAPosition>();

        public Client(ILogger<Client> logger, IHandeler handler, IConfiguration configRoot)
        {
            this.handler = handler;
            this.logger = logger;

            var config = configRoot.GetSection("ctrader");
            appid = config.GetValue<string>("appid");
            secret = config.GetValue<string>("secret");
            token = config.GetValue<string>("token");

            Connect().Wait();
        }

        private void CreateApp()
        {
            string appId = appid;
            string appSecret = secret;

            _token = new Token
            {
                AccessToken = token
            };

            _app = new App(appId, appSecret, string.Empty);
        }

        private async void CreateClient()
        {
            Mode mode = Mode.Demo;
            bool useWebScoket = true;


            string host = ApiInfo.GetHost(mode);
            _client = new OpenAPI.Net.OpenClient(host, ApiInfo.Port, TimeSpan.FromSeconds(10), useWebSocket: useWebScoket);

            _disposables.Add(_client.Where(iMessage => iMessage is not ProtoHeartbeatEvent).Subscribe(OnMessageReceived, OnException));
            _disposables.Add(_client.OfType<ProtoOARefreshTokenRes>().Subscribe(OnRefreshTokenResponse));

            await _client.Connect();
            logger.LogInformation($"Conencted to {host}");
        }

        private void OnRefreshTokenResponse(ProtoOARefreshTokenRes response)
        {
            _token = new Token
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                ExpiresIn = DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresIn),
                TokenType = response.TokenType,
            };
        }
        private void OnException(Exception obj)
        {
            logger.LogInformation(obj, "");
        }

        public async Task<bool> Connect()
        {
            CreateApp();
            CreateClient();

            var applicationAuthReq = new ProtoOAApplicationAuthReq
            {
                ClientId = _app.ClientId,
                ClientSecret = _app.Secret,
            };
            await _client.SendMessage(applicationAuthReq);

            await Task.Delay(500);
            handler.GetClients(_token, _client);

            await Task.Delay(1000);

            return true;
        }


        public void Dispose()
        {

        }

        public List<Quote> GetQuotes(string symbolName, string timeframe = "5m", int Size = 100)
        {
            throw new NotImplementedException();
        }

        public void OnMessageReceived(IMessage message)
        {
            if (message is ProtoOAGetAccountListByAccessTokenRes)
            {
                traderId = handler.OnAuth((ProtoOAGetAccountListByAccessTokenRes)message);
                AccountAuthRequest(traderId);
            }
            else if (message is ProtoOAExecutionEvent)
            {
                handler.OnExecutionEvent((ProtoOAExecutionEvent)message, this);
            }
            else if (message is ProtoOASpotEvent)
            {
                handler.OnTick((ProtoOASpotEvent)message);
            }
            else if (message is ProtoOAGetTrendbarsRes)
            {
                handler.OnTrends((ProtoOAGetTrendbarsRes)message);
            }
            else if (message is ProtoOATrailingSLChangedEvent)
            {

            }
            else if (message is ProtoMessage)
            {

            }
            else if (message is ProtoOASubscribeSpotsRes)
            {

            }
            else if (message is ProtoOADealListByPositionIdRes)
            {
                // Show positions
                logger.LogInformation($"Received message: {message}");
            }

            else
            {
                logger.LogInformation($"Received message: {message}");
                logger.LogInformation($"{message.GetType()}");
            }
        }

        private async void AccountAuthRequest(long accountId)
        {
            var request = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = accountId,
                AccessToken = _token.AccessToken
            };

            await _client.SendMessage(request);
        }

        #region Market
        public void CreateNewMarketOrder(ProtoOATradeSide TradeSide, long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0, decimal TakeProfit = 0)
        {
            logger.LogInformation($"{TradeSide} {SymbolId} {StopLoss} {Volume}");
            var newOrderReq = new ProtoOANewOrderReq
            {
                OrderType = ProtoOAOrderType.Market,
                CtidTraderAccountId = traderId,
                SymbolId = SymbolId,
                Volume = Volume,
                TradeSide = TradeSide
            };

            if (StopLoss != 0)
            {
                newOrderReq.RelativeStopLoss = (long)((StopLoss));
                newOrderReq.RelativeTakeProfit = (long)((TakeProfit));
                // newOrderReq.RelativeStopLoss = (long)(StopLoss / (PipSize * 1));
            }

            if (string.IsNullOrWhiteSpace(Label) is not true)
            {
                newOrderReq.Label = Label;
            }

            if (string.IsNullOrWhiteSpace(Comment) is not true)
            {
                newOrderReq.Comment = Comment;
            }

            _client.SendMessage(newOrderReq);
        }
        public void CancelPosition(long SymbolId, long Volume)
        {
            var request = new ProtoOAClosePositionReq
            {
                CtidTraderAccountId = traderId,
                Volume = Volume,
                PositionId = SymbolId
            };
            _client.SendMessage(request);
        }
        public void Buy(long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0, decimal TakeProfit = 0)
        {
            var v = Positions.Values.Where(x => x.TradeData.SymbolId == SymbolId).ToList();
            var c = v.Where(x => x.TradeData.TradeSide == ProtoOATradeSide.Sell).ToList();
            var d = v.Where(x => x.TradeData.TradeSide == ProtoOATradeSide.Buy).ToList();
            if (c.Count != 0)
            {
                foreach (var item in c)
                {
                    CancelPosition(item.PositionId, Volume);
                }
            }
            if (d.Count == 0)
            {
                CreateNewMarketOrder(ProtoOATradeSide.Buy, SymbolId, Volume, Label, Comment, StopLoss, TakeProfit);
            }
        }
        public void Sell(long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0, decimal TakeProfit = 0)
        {
            var v = Positions.Values.Where(x => x.TradeData.SymbolId == SymbolId).ToList();
            var c = v.Where(x => x.TradeData.TradeSide == ProtoOATradeSide.Buy).ToList();
            var d = v.Where(x => x.TradeData.TradeSide == ProtoOATradeSide.Sell).ToList();
            if (c.Count != 0)
            {
                foreach (var item in c)
                {
                    CancelPosition(item.PositionId, Volume);
                }
            }
            if (d.Count == 0)
            {
                CreateNewMarketOrder(ProtoOATradeSide.Sell, SymbolId, Volume, Label, Comment, StopLoss, TakeProfit);
            }
        }
        #endregion

        public async void Subscribe(long symbolName, string timeframe, int delay)
        {
            await Task.Delay(delay * 1000);
            handler.Subscribe(_token, _client, (ProtoOATrendbarPeriod)System.Enum.Parse(typeof(ProtoOATrendbarPeriod), timeframe, true), symbolName, traderId);
        }

        public async void Subscribe(long symbolName, string timeframe, IStrategy strategy, int delay)
        {
            await Task.Delay(delay * 1000);
            handler.Subscribe(_token, _client, (ProtoOATrendbarPeriod)System.Enum.Parse(typeof(ProtoOATrendbarPeriod), timeframe, true), symbolName, traderId, strategy);
        }

        public void GetPositions()
        {
            handler.GetPositions(_client, traderId);
        }
    }
}
