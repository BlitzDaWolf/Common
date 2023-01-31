﻿using Common;
using Google.Protobuf;
using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Helpers;
using OpenAPICtrader.Interface;
using Skender.Stock.Indicators;
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
        private readonly List<IDisposable> _disposables = new();

        public Client(ILogger<Client> logger, IHandeler handler)
        {
            this.handler = handler;
            this.logger = logger;

            Connect().Wait();
        }

        private void CreateApp()
        {
            string appId = "5160_eXumDqvZglpLaJMYzmm3MeRgMAgRVELgg7Q1QN3K3osATWxNbU";
            string appSecret = "EM8V2ioaBOX6Jwcw4Kca2X9IIwjYQ06IW269ggTUN9pm3lwuvX";

            _token = new Token
            {
                AccessToken = "i2YlbftJf_XJQR_xGgmY_zv5Xw2oj3lns50hhojfAnM"
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
                handler.OnExecutionEvent((ProtoOAExecutionEvent)message);
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
        public void CreateNewMarketOrder(ProtoOATradeSide TradeSide, long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0)
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
                newOrderReq.TrailingStopLoss = true;
                newOrderReq.RelativeStopLoss = (long)((int)(StopLoss));
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
        public void Buy(long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0) => CreateNewMarketOrder(ProtoOATradeSide.Buy, SymbolId, Volume, Label, Comment, StopLoss);
        public void Sell(long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0) => CreateNewMarketOrder(ProtoOATradeSide.Sell, SymbolId, Volume, Label, Comment, StopLoss);
        #endregion

        public void Subscribe(long symbolName, string timeframe)
        {
            handler.Subscribe(_token, _client, (ProtoOATrendbarPeriod)System.Enum.Parse(typeof(ProtoOATrendbarPeriod), timeframe, true), symbolName, traderId);
        }

        public void Subscribe(long symbolName, string timeframe, IStrategy strategy)
        {
            handler.Subscribe(_token, _client, (ProtoOATrendbarPeriod)System.Enum.Parse(typeof(ProtoOATrendbarPeriod), timeframe, true), symbolName, traderId, strategy);
        }
    }
}
