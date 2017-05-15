using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for monitoring a specific <see cref="PerformanceCounter"/>
    /// </summary>
    public class PerformanceCounterActor : ReceiveActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private PerformanceCounter _counter;

        private readonly HashSet<IActorRef> _subscriptions;
        private readonly ICancelable _cancelPublishing;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;
            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);

            Receive<GatherMetrics>(m => Handle(m));
            Receive<SubscribeCounter>(m => Handle(m));
            Receive<UnsubscribeCounter>(m => Handle(m));
        }

        #region Actor lifecycle methods

        protected override void PreStart()
        {
            _counter = _performanceCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                Self,
                new GatherMetrics(),
                Self,
                _cancelPublishing
                );
        }

        protected override void PostStop()
        {
            try
            {
                // termina the scheduled task
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch
            {
                // ignore exception
            }
            finally
            {
                base.PostStop();
            }
        }

        #endregion

        private void Handle(GatherMetrics message)
        {
            //publish latest counter value to all subscribers
            var metric = new Metric(_seriesName, _counter.NextValue());
            foreach (var sub in _subscriptions)
                sub.Tell(metric);
        }

        private void Handle(SubscribeCounter message)
        {
            // add a subscription for this counter
            // (it's parent's job to filter by counter types)
            _subscriptions.Add(message.Subscriber);
        }

        private void Handle(UnsubscribeCounter message)
        {
            _subscriptions.Remove(message.Subscriber);
        }
    }
}
