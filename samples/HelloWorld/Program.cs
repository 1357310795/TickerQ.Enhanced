using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TickerQ.DependencyInjection;
using TickerQ.DependencyInjection.Hosting;
using TickerQ.HelloWorld;
using TickerQ.Utilities;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models;
using TickerQ.Utilities.Models.Ticker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTickerQ();
builder.Services.AddSingleton<ITickerQNotificationHubSender, MyTickerQNotificationHubSender>();

var host = builder.Build();

host.UseTickerQ();

// Act
using (var scope = host.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    var a = Task.Delay(1000).ContinueWith(_ =>
    {
        var manager = scope.ServiceProvider.GetRequiredService<ITimeTickerManager<TimeTicker>>();
        //var manager2 = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTicker>>();
        //manager2.AddAsync(new CronTicker
        //{
        //    Id = Guid.NewGuid(),
        //    Function = nameof(BackgroundService.HelloWorld),
        //    Expression = "* * * * * *"
        //});
        manager.AddAsync(new TimeTicker()
        {
            Id = new Guid("2d88a5bc-2c40-4e6b-a802-b288f23f20ae"),
            Function = nameof(BackgroundService.HelloWorld),
            ExecutionTime = DateTime.UtcNow.AddSeconds(5),
        });
    });
    var b = Task.Delay(2000).ContinueWith(_ =>
    {
        var manager = scope.ServiceProvider.GetRequiredService<ITimeTickerManager<TimeTicker>>();
        manager.PauseAsync(new Guid("2d88a5bc-2c40-4e6b-a802-b288f23f20ae"));
    });
    var c = Task.Delay(3000).ContinueWith(_ =>
    {
        var manager = scope.ServiceProvider.GetRequiredService<ITimeTickerManager<TimeTicker>>();
        manager.ResumeAsync(new Guid("2d88a5bc-2c40-4e6b-a802-b288f23f20ae"));
    });
    await Task.WhenAll(a, b, c);
}

await host.RunAsync();

class BackgroundService
{
    // Prints Hello World every 1 minute.
    [TickerFunction(functionName: nameof(HelloWorld))]
    public void HelloWorld(TickerFunctionContext context)
    {
        Console.WriteLine("Hello World!");
        context.Result = "wwwwww";
    }
}

