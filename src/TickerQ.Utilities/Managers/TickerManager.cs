using Microsoft.Extensions.Logging;
using NCrontab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickerQ.Utilities.DashboardDtos;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Exceptions;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models;
using TickerQ.Utilities.Models.Ticker;

namespace TickerQ.Utilities.Managers
{
    internal class
        TickerManager<TTimeTicker, TCronTicker> :
        InternalTickerManager<TTimeTicker, TCronTicker>, ICronTickerManager<TCronTicker>,
        ITimeTickerManager<TTimeTicker>
        where TTimeTicker : TimeTicker, new() where TCronTicker : CronTicker, new()
    {
        public TickerManager(ITickerPersistenceProvider<TTimeTicker, TCronTicker> persistenceProvider,
            ITickerHost tickerHost, ITickerClock clock,
            TickerOptionsBuilder tickerOptions, ITickerQNotificationHubSender notificationHubSender,
            ILogger<InternalTickerManager<TTimeTicker, TCronTicker>> logger)
            : base(persistenceProvider, tickerHost, clock, tickerOptions, notificationHubSender, logger)
        {
            
        }

        Task<TickerResult<TCronTicker>> ICronTickerManager<TCronTicker>.AddAsync(TCronTicker entity,
            CancellationToken cancellationToken)
            => AddCronTickerAsync(entity, cancellationToken);

        Task<TickerResult<TTimeTicker>> ITimeTickerManager<TTimeTicker>.AddAsync(TTimeTicker entity,
            CancellationToken cancellationToken)
            => AddTimeTickerAsync(entity, cancellationToken);

        Task<TickerResult<TCronTicker>> ICronTickerManager<TCronTicker>.UpdateAsync(Guid id,
            Action<TCronTicker> updateAction, CancellationToken cancellationToken)
            => UpdateCronTickerAsync(id, updateAction, cancellationToken);

        Task<TickerResult<TTimeTicker>> ITimeTickerManager<TTimeTicker>.UpdateAsync(Guid id, Action<TTimeTicker> action,
            CancellationToken cancellationToken)
            => UpdateTimeTickerAsync(id, action, cancellationToken);

        Task<TickerResult<TCronTicker>> ICronTickerManager<TCronTicker>.DeleteAsync(Guid id,
            CancellationToken cancellationToken)
            => DeleteCronTickerAsync(id, cancellationToken);

        Task<TickerResult<TTimeTicker>> ITimeTickerManager<TTimeTicker>.DeleteAsync(Guid id,
            CancellationToken cancellationToken)
            => DeleteTimeTickerAsync(id, cancellationToken);

        Task<TickerResult<TCronTicker>> ICronTickerManager<TCronTicker>.GetAsync(Guid id,
            CancellationToken cancellationToken)
            => GetCronTickerAsync(id, cancellationToken);

        Task<TickerResult<TTimeTicker>> ITimeTickerManager<TTimeTicker>.GetAsync(Guid id,
            CancellationToken cancellationToken)
            => GetTimeTickerAsync(id, cancellationToken);

        Task<TickerResult<TCronTicker>> ICronTickerManager<TCronTicker>.PauseAsync(Guid id,
            CancellationToken cancellationToken)
            => PauseCronTickerAsync(id, cancellationToken);

        Task<TickerResult<TTimeTicker>> ITimeTickerManager<TTimeTicker>.PauseAsync(Guid id,
            CancellationToken cancellationToken)
            => PauseTimeTickerAsync(id, cancellationToken);

        Task<TickerResult<TCronTicker>> ICronTickerManager<TCronTicker>.ResumeAsync(Guid id,
            CancellationToken cancellationToken)
            => ResumeCronTickerAsync(id, cancellationToken);

        Task<TickerResult<TTimeTicker>> ITimeTickerManager<TTimeTicker>.ResumeAsync(Guid id,
            CancellationToken cancellationToken)
            => ResumeTimeTickerAsync(id, cancellationToken);

        Task ICronTickerManager<TCronTicker>.TriggerAsync(Guid id, Dictionary<string, string> additionalDataMap, CancellationToken cancellationToken)
            => TriggerCronTickerAsync(id, additionalDataMap, cancellationToken);

        Task ITimeTickerManager<TTimeTicker>.TriggerAsync(Guid id, Dictionary<string, string> additionalDataMap, CancellationToken cancellationToken)
            => TriggerTimeTickerAsync(id, additionalDataMap, cancellationToken);

        private async Task<TickerResult<TTimeTicker>> AddTimeTickerAsync(TTimeTicker entity,
            CancellationToken cancellationToken)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();
            
            if (TickerFunctionProvider.TickerFunctions.All(x => x.Key != entity?.Function))
                return new TickerResult<TTimeTicker>(
                    new TickerValidatorException($"Cannot find TickerFunction with name {entity?.Function}"));

            try
            {
                if (entity.ExecutionTime == default)
                    return new TickerResult<TTimeTicker>(new TickerValidatorException("Invalid ExecutionTime!"));

                entity.CreatedAt = Clock.UtcNow;
                entity.UpdatedAt = Clock.UtcNow;
                entity.Status = Clock.UtcNow > entity.ExecutionTime ? TickerStatus.DueDone : TickerStatus.Idle;//entity.BatchParent != null ? TickerStatus.Batched : TickerStatus.Idle;
                entity.ExecutionTime = entity.ExecutionTime.ToUniversalTime();

                await PersistenceProvider.InsertTimeTickers(new[] { entity }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                TickerHost.RestartIfNeeded(entity.ExecutionTime);

                if (NotificationHubSender != null)
                    await NotificationHubSender.AddTimeTickerNotifyAsync(new TimeTickerDto
                    {
                        Id = entity.Id,
                        Function = entity.Function,
                        ExecutionTime = entity.ExecutionTime,
                        Status = entity.Status,
                        Exception = entity.Exception,
                        ElapsedTime = entity.ElapsedTime,
                        CreatedAt = entity.CreatedAt,
                        UpdatedAt = entity.UpdatedAt,
                        LockedAt = entity.LockedAt,
                        LockHolder = entity.LockHolder,
                        Description = entity.Description,
                        Retries = entity.Retries,
                        RetryIntervals = entity.RetryIntervals,
                        ExecutedAt = entity.ExecutedAt,
                        RetryCount = entity.RetryCount,
                        BatchRunCondition = entity.BatchRunCondition,
                        BatchParent = entity.BatchParent
                    });

                return new TickerResult<TTimeTicker>(entity);
            }
            catch (Exception e)
            {
                return new TickerResult<TTimeTicker>(e);
            }
        }

        private async Task<TickerResult<TCronTicker>> AddCronTickerAsync(TCronTicker entity,
            CancellationToken cancellationToken)
        {
            try
            {
                if(entity.Id == Guid.Empty)
                    entity.Id = Guid.NewGuid();
                
                if (TickerFunctionProvider.TickerFunctions.All(x => x.Key != entity?.Function))
                    return new TickerResult<TCronTicker>(
                        new TickerValidatorException($"Cannot find TickerFunction with name {entity?.Function}"));

                if (!(CrontabSchedule.TryParse(entity.Expression, new CrontabSchedule.ParseOptions() { IncludingSeconds = true }) is { } crontabSchedule))
                    return new TickerResult<TCronTicker>(
                        new TickerValidatorException($"Cannot parse expression {entity.Expression}"));

                var nextOccurrence = crontabSchedule.GetNextOccurrence(Clock.UtcNow);

                entity.CreatedAt = Clock.UtcNow;
                entity.UpdatedAt = Clock.UtcNow;

                await PersistenceProvider.InsertCronTickers(new[] { entity }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var generateNextOccurrence = new CronTickerOccurrence<TCronTicker>
                {
                    Status = TickerStatus.Idle,
                    ExecutionTime = nextOccurrence,
                    LockedAt = Clock.UtcNow,
                    LockHolder = LockHolder,
                    CronTicker = entity,
                    CronTickerId = entity.Id
                };

                await PersistenceProvider
                    .InsertCronTickerOccurrences(new[] { generateNextOccurrence }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                TickerHost.RestartIfNeeded(nextOccurrence);

                if (NotificationHubSender != null)
                    await NotificationHubSender.AddCronTickerNotifyAsync(new CronTickerDto
                    {
                        Function = entity.Function,
                        Expression = entity.Expression,
                        ExpressionReadable = entity.Expression.ToHumanReadable(),
                        UpdatedAt = entity.UpdatedAt,
                        CreatedAt = entity.CreatedAt,
                        Retries = entity.Retries,
                        RetryIntervals = entity.RetryIntervals,                        
                        Id = entity.Id
                    });

                return new TickerResult<TCronTicker>(entity);
            }
            catch (Exception e)
            {
                return new TickerResult<TCronTicker>(e);
            }
        }

        private async Task<TickerResult<TTimeTicker>> UpdateTimeTickerAsync(Guid id, Action<TTimeTicker> updateAction,
            CancellationToken cancellationToken)
        {
            try
            {
                var timeTicker = await PersistenceProvider.GetTimeTickerById(id, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (timeTicker == null)
                    return new TickerResult<TTimeTicker>(
                        new TickerValidatorException($"Cannot find TimeTicker with id {id}!"));

                updateAction(timeTicker);

                timeTicker.UpdatedAt = Clock.UtcNow;
                timeTicker.ExecutionTime = timeTicker.ExecutionTime.ToUniversalTime();

                if (timeTicker.Status == TickerStatus.Queued)
                    TickerHost.Restart();
                else
                    TickerHost.RestartIfNeeded(timeTicker.ExecutionTime);

                await PersistenceProvider.UpdateTimeTickers(new[] { timeTicker }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return new TickerResult<TTimeTicker>(timeTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TTimeTicker>(e);
            }
        }

        private async Task<TickerResult<TCronTicker>> UpdateCronTickerAsync(Guid id,
            Action<TCronTicker> updateAction,
            CancellationToken cancellationToken = default)
        {
            var cronTicker = await PersistenceProvider.GetCronTickerById(id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (cronTicker == null)
                return new TickerResult<TCronTicker>(new Exception($"Cannot find CronTicker with id {id}!"));

            if (TickerFunctionProvider.TickerFunctions.All(x => x.Key != cronTicker?.Function))
                return new TickerResult<TCronTicker>(
                    new TickerValidatorException($"Cannot find TickerFunction with name {cronTicker.Function}"));

            try
            {
                var cronTickerExpression = cronTicker.Expression;
                var function = cronTicker.Function;

                updateAction(cronTicker);

                var coreChanges = (cronTickerExpression != cronTicker.Expression) || function != cronTicker.Function;

                if (!(CrontabSchedule.TryParse(cronTicker.Expression, new CrontabSchedule.ParseOptions() { IncludingSeconds = true }) is { } crontabSchedule))
                    return new TickerResult<TCronTicker>(
                        new TickerValidatorException($"Cannot parse expression {cronTicker.Expression}"));

                cronTicker.UpdatedAt = Clock.UtcNow;

                await PersistenceProvider.UpdateCronTickers(new[] { cronTicker }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (coreChanges)
                {
                    var occurrencesToRemove = await PersistenceProvider.GetCronOccurrencesByCronTickerIdAndStatusFlag(
                        cronTicker.Id,
                        new[]
                        {
                            TickerStatus.Idle,
                            TickerStatus.Queued
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (occurrencesToRemove.Length > 0)
                    {
                        foreach (var o in occurrencesToRemove)
                            o.Status = TickerStatus.Cancelled;
                        await PersistenceProvider
                            .UpdateCronTickerOccurrences(occurrencesToRemove, cancellationToken: cancellationToken);
                    }

                    var generateNextOccurrence = new CronTickerOccurrence<TCronTicker>
                    {
                        CronTickerId = cronTicker.Id,
                        Status = TickerStatus.Idle,
                        ExecutionTime = crontabSchedule.GetNextOccurrence(Clock.UtcNow),
                        LockedAt = Clock.UtcNow,
                        LockHolder = LockHolder,
                        CronTicker = cronTicker
                    };

                    await PersistenceProvider
                        .InsertCronTickerOccurrences(new[] { generateNextOccurrence },
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    TickerHost.Restart();
                }

                return new TickerResult<TCronTicker>(cronTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TCronTicker>(e);
            }
        }

        private async Task<TickerResult<TCronTicker>> DeleteCronTickerAsync(Guid id,
            CancellationToken cancellationToken = default)
        {
            var cronTicker = await PersistenceProvider.GetCronTickerById(id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (cronTicker == null)
                return new TickerResult<TCronTicker>(
                    new TickerValidatorException($"Cannot find CronTicker with id {id}!"));

            try
            {
                var queuedCronOccurrences =
                    await PersistenceProvider.GetQueuedNextCronOccurrences(cronTicker.Id,
                        cancellationToken: cancellationToken);

                await PersistenceProvider.RemoveCronTickers(new[] { cronTicker }, cancellationToken: cancellationToken);

                if (queuedCronOccurrences.Any())
                    TickerHost.Restart();

                return new TickerResult<TCronTicker>(cronTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TCronTicker>(e);
            }
        }

        private async Task<TickerResult<TTimeTicker>> DeleteTimeTickerAsync(Guid id,
            CancellationToken cancellationToken = default)
        {
            var timeTicker = await PersistenceProvider.GetTimeTickerById(id, cancellationToken: cancellationToken);

            if (timeTicker == null)
                return new TickerResult<TTimeTicker>(
                    new TickerValidatorException($"Cannot find TimeTicker with id {id}!"));

            try
            {
                await PersistenceProvider.RemoveTimeTickers(new[] { timeTicker }, cancellationToken: cancellationToken);

                if (timeTicker.Status == TickerStatus.Queued)
                    TickerHost.Restart();

                return new TickerResult<TTimeTicker>(timeTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TTimeTicker>(e);
            }
        }

        private async Task<TickerResult<TCronTicker>> GetCronTickerAsync(Guid id,
            CancellationToken cancellationToken = default)
        {
            var cronTicker = await PersistenceProvider.GetCronTickerById(id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (cronTicker == null)
                return new TickerResult<TCronTicker>(
                    new TickerValidatorException($"Cannot find CronTicker with id {id}!"));
            return new TickerResult<TCronTicker>(cronTicker);
        }

        private async Task<TickerResult<TTimeTicker>> GetTimeTickerAsync(Guid id,
            CancellationToken cancellationToken = default)
        {
            var timeTicker = await PersistenceProvider.GetTimeTickerById(id, cancellationToken: cancellationToken);

            if (timeTicker == null)
                return new TickerResult<TTimeTicker>(
                    new TickerValidatorException($"Cannot find TimeTicker with id {id}!"));
            return new TickerResult<TTimeTicker>(timeTicker);
        }

        private async Task TriggerCronTickerAsync(Guid id, Dictionary<string, string> additionalDataMap = default, CancellationToken cancellationToken = default)
        {
            var cronTicker = await PersistenceProvider.GetCronTickerById(id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (cronTicker == null)
                throw new TickerValidatorException($"Cannot find CronTicker with id {id}!");

            if (!TickerFunctionProvider.TickerFunctions.TryGetValue(cronTicker.Function, out var tickerItem))
                throw new Exception($"Cannot find TickerFunctions in CronTicker id {id}!");

            var context = new InternalFunctionContext()
            {
                FunctionName = cronTicker.Function,
                TickerId = cronTicker.Id,
                OccurrenceId = Guid.Empty,
                Type = TickerType.Timer,
                Retries = cronTicker.Retries,
                RetryIntervals = cronTicker.RetryIntervals,
                DataMap = additionalDataMap.Concat(cronTicker.DataMap)
                    .GroupBy(kvp => kvp.Key)
                    .ToDictionary(g => g.Key, g => g.First().Value)
            };

            await Task.Factory.StartNew(
                async () => await TickerHost.ExecuteTaskAsync(context, tickerItem.Delegate, false, cancellationToken));
        }

        private async Task TriggerTimeTickerAsync(Guid id, Dictionary<string, string> additionalDataMap = default, CancellationToken cancellationToken = default)
        {
            var timeTicker = await PersistenceProvider.GetTimeTickerById(id, cancellationToken: cancellationToken);

            if (timeTicker == null)
                throw new TickerValidatorException($"Cannot find TimeTicker with id {id}!");

            if (!TickerFunctionProvider.TickerFunctions.TryGetValue(timeTicker.Function, out var tickerItem))
                throw new Exception($"Cannot find TickerFunctions in CronTicker id {id}!");

            var context = new InternalFunctionContext()
            {
                FunctionName = timeTicker.Function,
                TickerId = timeTicker.Id,
                OccurrenceId = Guid.Empty,
                Type = TickerType.Timer,
                Retries = timeTicker.Retries,
                RetryIntervals = timeTicker.RetryIntervals,
                DataMap = additionalDataMap.Concat(timeTicker.DataMap)
                    .GroupBy(kvp => kvp.Key)
                    .ToDictionary(g => g.Key, g => g.First().Value)
            };

            await Task.Factory.StartNew(
                async () => await TickerHost.ExecuteTaskAsync(context, tickerItem.Delegate, false, cancellationToken));
        }

        private async Task<TickerResult<TTimeTicker>> PauseTimeTickerAsync(Guid id,
            CancellationToken cancellationToken)
        {
            try
            {
                var timeTicker = await PersistenceProvider.GetTimeTickerById(id, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (timeTicker == null)
                    return new TickerResult<TTimeTicker>(
                        new TickerValidatorException($"Cannot find TimeTicker with id {id}!"));

                timeTicker.IsPaused = true;
                timeTicker.UpdatedAt = Clock.UtcNow;
                timeTicker.ExecutionTime = timeTicker.ExecutionTime.ToUniversalTime();

                await PersistenceProvider.UpdateTimeTickers(new[] { timeTicker }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (timeTicker.Status == TickerStatus.Queued)
                    TickerHost.RestartThrottled();

                return new TickerResult<TTimeTicker>(timeTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TTimeTicker>(e);
            }
        }

        private async Task<TickerResult<TCronTicker>> PauseCronTickerAsync(Guid id,
            CancellationToken cancellationToken = default)
        {
            var cronTicker = await PersistenceProvider.GetCronTickerById(id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (cronTicker == null)
                return new TickerResult<TCronTicker>(new Exception($"Cannot find CronTicker with id {id}!"));

            try
            {
                var cronTickerExpression = cronTicker.Expression;
                var function = cronTicker.Function;

                cronTicker.IsPaused = true;
                cronTicker.UpdatedAt = Clock.UtcNow;

                await PersistenceProvider.UpdateCronTickers(new[] { cronTicker }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var occurrencesToRemove = await PersistenceProvider.GetCronOccurrencesByCronTickerIdAndStatusFlag(
                    cronTicker.Id,
                    new[]
                    {
                        TickerStatus.Idle,
                        TickerStatus.Queued
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (occurrencesToRemove.Length > 0)
                {
                    foreach (var o in occurrencesToRemove)
                        o.Status = TickerStatus.Cancelled;
                    await PersistenceProvider
                        .UpdateCronTickerOccurrences(occurrencesToRemove, cancellationToken: cancellationToken);
                }

                TickerHost.RestartThrottled();
                
                return new TickerResult<TCronTicker>(cronTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TCronTicker>(e);
            }
        }

        private async Task<TickerResult<TTimeTicker>> ResumeTimeTickerAsync(Guid id, 
            CancellationToken cancellationToken)
        {
            try
            {
                var timeTicker = await PersistenceProvider.GetTimeTickerById(id, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (timeTicker == null)
                    return new TickerResult<TTimeTicker>(
                        new TickerValidatorException($"Cannot find TimeTicker with id {id}!"));

                timeTicker.IsPaused = false;
                timeTicker.UpdatedAt = Clock.UtcNow;
                timeTicker.ExecutionTime = timeTicker.ExecutionTime.ToUniversalTime();

                await PersistenceProvider.UpdateTimeTickers(new[] { timeTicker }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                TickerHost.RestartThrottled();

                return new TickerResult<TTimeTicker>(timeTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TTimeTicker>(e);
            }
        }

        private async Task<TickerResult<TCronTicker>> ResumeCronTickerAsync(Guid id,
            CancellationToken cancellationToken = default)
        {
            var cronTicker = await PersistenceProvider.GetCronTickerById(id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (cronTicker == null)
                return new TickerResult<TCronTicker>(new Exception($"Cannot find CronTicker with id {id}!"));

            if (TickerFunctionProvider.TickerFunctions.All(x => x.Key != cronTicker?.Function))
                return new TickerResult<TCronTicker>(
                    new TickerValidatorException($"Cannot find TickerFunction with name {cronTicker.Function}"));

            try
            {
                var cronTickerExpression = cronTicker.Expression;
                var function = cronTicker.Function;

                cronTicker.IsPaused = false;
                cronTicker.UpdatedAt = Clock.UtcNow;

                if (!(CrontabSchedule.TryParse(cronTicker.Expression, new CrontabSchedule.ParseOptions() { IncludingSeconds = true }) is { } crontabSchedule))
                    return new TickerResult<TCronTicker>(
                        new TickerValidatorException($"Cannot parse expression {cronTicker.Expression}"));

                await PersistenceProvider.UpdateCronTickers(new[] { cronTicker }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var occurrencesToRemove = await PersistenceProvider.GetCronOccurrencesByCronTickerIdAndStatusFlag(
                    cronTicker.Id,
                    new[]
                    {
                        TickerStatus.Idle,
                        TickerStatus.Queued
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (occurrencesToRemove.Length > 0)
                {
                    foreach (var o in occurrencesToRemove)
                        o.Status = TickerStatus.Cancelled;
                    await PersistenceProvider
                        .UpdateCronTickerOccurrences(occurrencesToRemove, cancellationToken: cancellationToken);
                }

                var generateNextOccurrence = new CronTickerOccurrence<TCronTicker>
                {
                    CronTickerId = cronTicker.Id,
                    Status = TickerStatus.Idle,
                    ExecutionTime = crontabSchedule.GetNextOccurrence(Clock.UtcNow),
                    LockedAt = Clock.UtcNow,
                    LockHolder = LockHolder,
                    CronTicker = cronTicker
                };

                await PersistenceProvider
                    .InsertCronTickerOccurrences(new[] { generateNextOccurrence },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                TickerHost.RestartThrottled();
                
                return new TickerResult<TCronTicker>(cronTicker);
            }
            catch (Exception e)
            {
                return new TickerResult<TCronTicker>(e);
            }
        }
    }
}