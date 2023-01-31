using Common;
using Google.Protobuf;

namespace OpenAPICtrader.Interface
{
    public interface IOpenClient : IClient
    {
        IHandeler handler { get; set; }
        public void CreateNewMarketOrder(ProtoOATradeSide TradeSide, long SymbolId, long Volume, string? Label = "", string? Comment = "", decimal StopLoss = 0);
        void OnMessageReceived(IMessage message);
    }
}
