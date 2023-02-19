using Common;
using OpenAPICtrader.Interface;

namespace ServerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public IOpenClient Client { get; }

        public Worker(ILogger<Worker> logger, IOpenClient clnt, StrategyDispatcher strategy)
        {
            _logger = logger;
            Client = clnt;
        }

        public Worker()
        {

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /*await Client.Connect();
            Client.Subscribe(1, "m5");*/
            while (!stoppingToken.IsCancellationRequested)
            {
                // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                Client.GetPositions();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}