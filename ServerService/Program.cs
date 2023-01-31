using Common;
using OpenAPICtrader.Interface;
using ServerService;


Activator.CreateInstance(typeof(Worker));

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        
        services.AddSingleton<IHandeler, Handler>();
        services.AddSingleton<IOpenClient, Client>();

        services.AddSingleton<IStrategy, Strategy>();

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
