using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBrains.Annotations;

namespace TeacherAssistant.Services
{
    public class TimerService<TInterval, TEventTimeOffset> : IDisposable where TInterval : class, IInterval where TEventTimeOffset : class, IIntervalEvent
    {
        [NotNull] private readonly Subject<List<(TInterval, TEventTimeOffset)>> _eventStream =
            new Subject<List<(TInterval, TEventTimeOffset)>>();

        [CanBeNull] private IDisposable _activeTimer;
        [CanBeNull] private Dictionary<DateTime, List<(TInterval, TEventTimeOffset)>> _eventQueue;
        [CanBeNull] public LinkedList<(DateTime, List<TInterval>)> StartsQueue { get; private set; }
        [CanBeNull] public LinkedList<(DateTime, List<TInterval>)> EndsQueue { get; private set; }

        public void CreateSchedule([NotNull] IEnumerable<TInterval> timeIntervals,
            [NotNull] IEnumerable<TEventTimeOffset> everyIntervalEvents)
        {
            Stop();
            var intervals = timeIntervals.ToList();
            var intervalTimePoints = everyIntervalEvents.ToList();
            StartsQueue = new LinkedList<(DateTime, List<TInterval>)>(intervals.GroupBy(interval => interval.StartDateTime)
                .OrderBy(grouping => grouping.Key)
                .Select(grouping => (Date: grouping.Key, timeIntervals: grouping.ToList())));
            EndsQueue = new LinkedList<(DateTime, List<TInterval>)>(intervals.GroupBy(interval => interval.EndDateTime)
                .OrderBy(grouping => grouping.Key)
                .Select(grouping => (Date: grouping.Key, timeIntervals: grouping.ToList())));
            var beginTimePoints = intervalTimePoints
                .Where(point => point.RelativelyTo == StartPoint.Start);
            var endTimePoints = intervalTimePoints
                .Where(point => point.RelativelyTo == StartPoint.End);
            var eventsScheduledByBeginning = intervals
                .Select(interval => (interval, beginTimePoints))
                .Aggregate(new Dictionary<DateTime, List<(TInterval, TEventTimeOffset)>>(),
                    (dictionary, tuple) => AggregateEventsByTime(dictionary, tuple, StartPoint.Start)
                );
            var eventsScheduledByEnd = intervals
                .Select(interval => (interval, endTimePoints))
                .Aggregate(new Dictionary<DateTime, List<(TInterval, TEventTimeOffset)>>(),
                    (dictionary, tuple) => AggregateEventsByTime(dictionary, tuple, StartPoint.End)
                );
            _eventQueue = eventsScheduledByEnd.ToDictionary(pair => pair.Key, pair => pair.Value);
            eventsScheduledByBeginning
                .ToList()
                .ForEach(pair =>
                {
                    if (_eventQueue.ContainsKey(pair.Key))
                    {
                        _eventQueue[pair.Key].AddRange(pair.Value);
                    }
                    else
                    {
                        _eventQueue.Add(pair.Key, pair.Value);
                    }
                });
        }

        public IObservable<List<(TInterval, TEventTimeOffset)>> OnScheduled => _eventStream;
        public (DateTime, List<(TInterval, TEventTimeOffset)>)? NextEvent { get; private set; }
        [CanBeNull] public List<TInterval> NextStarts
        {
            get
            {
                var now = DateTime.Now;
                var next = StartsQueue?.FirstOrDefault(tuple => tuple.Item1 > now);
                return next?.Item2;
            }
        }

        [CanBeNull]
        public List<TInterval> NextEnds
        {
            get
            {
                var now = DateTime.Now;
                var next = EndsQueue?.FirstOrDefault(tuple => tuple.Item1 > now);
                return next?.Item2;
            }
        }

        public void Start()
        {
            if (_eventQueue == null)
            {
                return;
            }

            var now = DateTime.Now;
            var schedule = _eventQueue.Keys
                .OrderBy(time => time)
                .Where(time => time > now)
                .Aggregate(
                    new LinkedList<(DateTime, List<(TInterval, TEventTimeOffset)>)>(),
                    (list, time) =>
                    {
                        list.AddLast((time, _eventQueue[time]));
                        return list;
                    });
            StartTimer(schedule.First);
        }

        private void StartTimer([CanBeNull] LinkedListNode<(DateTime, List<(TInterval, TEventTimeOffset)>)> scheduledEventData)
        {
            if (scheduledEventData == null)
            {
                _eventQueue = null;
                NextEvent = null;
                return;
            }

            var (time, data) = scheduledEventData.Value;
            NextEvent = scheduledEventData.Value;
            _activeTimer = Observable.Timer(time)
                .Subscribe(_ =>
                {
                    StartTimer(scheduledEventData.Next);
                    _eventStream.OnNext(data);
                });
        }

        private Dictionary<DateTime, List<(TInterval, TEventTimeOffset)>> AggregateEventsByTime(
            Dictionary<DateTime, List<(TInterval, TEventTimeOffset)>> dictionary,
            (TInterval, IEnumerable<TEventTimeOffset>) tuple, StartPoint point)
        {
            var (time, offsets) = tuple;
            foreach (var timeOffset in offsets)
            {
                var eventTime = (point == StartPoint.Start ? time.StartDateTime : time.EndDateTime) +
                                timeOffset.TimeDiff;
                if (!dictionary.ContainsKey(eventTime))
                {
                    dictionary.Add(eventTime, new List<(TInterval, TEventTimeOffset)>());
                }

                dictionary[eventTime].Add((time, timeOffset));
            }

            return dictionary;
        }

        [CanBeNull]
        public List<TInterval> GetNextStartsAfter(TInterval interval)
        {
            var node = StartsQueue?.FirstOrDefault(tuple => tuple.Item2.Contains(interval));
            return node == default ? null : GetNextStartsAfter(node);
        }
        [CanBeNull]
        public List<TInterval> GetNextStartsAfter((DateTime, List<TInterval>)? interval)
        {
            if (!interval.HasValue)
            {
                return null;
            }

            var node = StartsQueue?.Find(interval.Value);

            return node?.Value.Item2;
        }

        public void Stop()
        {
            _eventQueue = null;
            NextEvent = null;
            if (_activeTimer == null) return;
            _activeTimer.Dispose();
            _activeTimer = null;
        }

        public void Dispose()
        {
            _activeTimer?.Dispose();
            _eventStream.Dispose();
        }
    }
}
