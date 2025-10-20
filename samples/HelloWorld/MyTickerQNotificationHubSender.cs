using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Models;
using TickerQ.Utilities.Models.Ticker;

namespace TickerQ.HelloWorld
{
    internal class MyTickerQNotificationHubSender : ITickerQNotificationHubSender
    {
        private readonly ITickerPersistenceProvider<TimeTicker, CronTicker> _persistenceProvider;

        public MyTickerQNotificationHubSender(ITickerPersistenceProvider<TimeTicker, CronTicker> persistenceProvider)
        {
            _persistenceProvider = persistenceProvider;
        }

        public Task AddCronOccurrenceAsync(Guid groupId, object occurrence)
        {
            return Task.CompletedTask;
        }

        public Task AddCronTickerNotifyAsync(object cronTicker)
        {
            return Task.CompletedTask;
        }

        public Task AddTimeTickerNotifyAsync(object timeTicker)
        {
            return Task.CompletedTask;
        }

        public Task CanceledTickerNotifyAsync(Guid id)
        {
            return Task.CompletedTask;
        }

        public async Task OnTickerExecutedAsync(InternalFunctionContext context)
        {
            if (context.Type == TickerType.Timer)
            {
                var timeTicker = await _persistenceProvider.GetTimeTickerById(context.TickerId);
                Console.WriteLine($"TimeTicker {timeTicker.Id} Executed");
            }
            else
            {
                var cronTicker = await _persistenceProvider.GetCronTickerByOccurrenceId(context.TickerId);
                Console.WriteLine($"CronTicker {cronTicker.Id} Executed");
            }
            Console.WriteLine($"Result: {context.Result}");
        }

        public async Task OnTickerExecutingAsync(InternalFunctionContext context)
        {
            if (context.Type == TickerType.Timer)
            {
                var timeTicker = await _persistenceProvider.GetTimeTickerById(context.TickerId);
                Console.WriteLine($"TimeTicker {timeTicker.Id} Executing");
            }
            else
            {
                var cronTicker = await _persistenceProvider.GetCronTickerByOccurrenceId(context.TickerId);
                Console.WriteLine($"CronTicker {cronTicker.Id} Executing");
            }
            Console.WriteLine($"Result: {context.Result}");
        }

        public Task RemoveCronTickerNotifyAsync(Guid id)
        {
            return Task.CompletedTask;
        }

        public Task RemoveTimeTickerNotifyAsync(Guid id)
        {
            return Task.CompletedTask;
        }

        public void UpdateActiveThreads(int activeThreads)
        {
            return;
        }

        public Task UpdateCronOccurrenceAsync(Guid groupId, object occurrence)
        {
            return Task.CompletedTask;
        }

        public Task UpdateCronTickerNotifyAsync(object cronTicker)
        {
            return Task.CompletedTask;
        }

        public void UpdateHostException(string exceptionMessage)
        {
            return;
        }

        public void UpdateHostStatus(bool active)
        {
            return;
        }

        public void UpdateNextOccurrence(DateTime? nextOccurrence)
        {
            return;
        }

        public Task UpdateTimeTickerNotifyAsync(object timeTicker)
        {
            return Task.CompletedTask;
        }
    }
}
