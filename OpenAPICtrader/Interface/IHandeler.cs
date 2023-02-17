using Common;
using OpenAPI.Net;
using OpenAPI.Net.Auth;

namespace OpenAPICtrader.Interface
{
    public interface IHandeler
    {
        Type SymbolHandlerType { get; }
        Dictionary<long, ISymbolHandler> Symbols { get; }

        void OnTrends(ProtoOAGetTrendbarsRes message);
        void OnTick(ProtoOASpotEvent message);
        void OnTrend(ProtoOASpotEvent message);
        void OnExecutionEvent(ProtoOAExecutionEvent message, IOpenClient client);

        long OnAuth(ProtoOAGetAccountListByAccessTokenRes message);

        void Subscribe(Token token, OpenClient client, ProtoOATrendbarPeriod timeframe, long symbolId, long traderId);
        void Subscribe(Token token, OpenClient client, ProtoOATrendbarPeriod timeframe, long symbolId, long traderId, IStrategy strategy);
        void AccountAuthRequest(Token token, OpenClient client, long accountId);
        void GetClients(Token token, OpenClient client);
        void GetPositions(OpenClient client, long traderId);
    }
}
