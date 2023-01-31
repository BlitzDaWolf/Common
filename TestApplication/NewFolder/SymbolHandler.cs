using Common;

namespace TestApplication.NewFolder
{
    internal class SymbolHandler : ISymbolHandler
    {
        public long SymbolId { get; }

        public Dictionary<string, List<OLHC>> Bars { get; } = new Dictionary<string, List<OLHC>>();
        public Dictionary<string, OLHC> CurrentBar { get; } = new Dictionary<string, OLHC>();
        public Dictionary<string, IStrategy> Strategies { get; } = new Dictionary<string, IStrategy>();

        public SymbolHandler(long symbolId)
        {
            SymbolId = symbolId;
        }
    }
}
