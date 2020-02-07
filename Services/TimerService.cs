using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBrains.Annotations;
using Model;
using Model.Models;

namespace TeacherAssistant
{
    public enum StartPoint
    {
        Start,
        End
    }

    public interface IInterval
    {
        DateTime StartDateTime { get; }
        DateTime EndDateTime { get; }
    }

    public interface IIntervalTimePoint
    {
        StartPoint RelativelyTo { get; }
        TimeSpan TimeDiff { get; }
    }

    public class TimerService<T, TOffset> : IDisposable where T : IInterval where TOffset : IIntervalTimePoint
    {
        [NotNull] private readonly Subject<List<(T, TOffset)>> _eventStream =
            new Subject<List<(T, TOffset)>>();

        [CanBeNull] private IDisposable _activeTimer;
        [CanBeNull] private Dictionary<DateTime, List<(T, TOffset)>> _eventQueue;
        [CanBeNull] private Dictionary<DateTime, List<T>> _startsQueue;
        [CanBeNull] private Dictionary<DateTime, List<T>> _endsQueue;

        public void CreateSchedule([NotNull] IEnumerable<T> timeIntervals,
            [NotNull] IEnumerable<TOffset> everyLessonTimer)
        {
            Stop(); // todo make easier
            var intervals = timeIntervals.ToList();
            var intervalTimePoints = everyLessonTimer.ToList();
            _startsQueue = intervals.Aggregate(new Dictionary<DateTime, List<T>>(),
                (dictionary, interval) =>
                {
                    return dictionary;
                });
            var beginTimePoints = intervalTimePoints
                .Where(point => point.RelativelyTo == StartPoint.Start);
            var endTimePoints = intervalTimePoints
                .Where(point => point.RelativelyTo == StartPoint.End);
            var eventsScheduledByBeginning = intervals
                .Select(interval => (interval, beginTimePoints))
                .Aggregate(new Dictionary<DateTime, List<(T, TOffset)>>(),
                    (dictionary, tuple) => AggregateEventsByTime(dictionary, tuple, StartPoint.Start)
                );
            var eventsScheduledByEnd = intervals
                .Select(interval => (interval, endTimePoints))
                .Aggregate(new Dictionary<DateTime, List<(T, TOffset)>>(),
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

        public IObservable<List<(T, TOffset)>> OnScheduled => _eventStream;
        public (DateTime, List<(T, TOffset)>)? NextEvent { get; private set; }
        [CanBeNull] public List<T> NextStarts { get; private set; }
        [CanBeNull] public List<T> NextEnds { get; private set; }

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
                    new LinkedList<(DateTime, List<(T, TOffset)>)>(),
                    (list, time) =>
                    {
                        list.AddLast((time, _eventQueue[time]));
                        return list;
                    });
            StartTimer(schedule.First);
        }

        private void StartTimer([CanBeNull] LinkedListNode<(DateTime, List<(T, TOffset)>)> scheduledEventData)
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

        private Dictionary<DateTime, List<(T, TOffset)>> AggregateEventsByTime(
            Dictionary<DateTime, List<(T, TOffset)>> dictionary,
            (T, IEnumerable<TOffset>) tuple, StartPoint point)
        {
            var (time, offsets) = tuple;
            foreach (var timeOffset in offsets)
            {
                var eventTime = (point == StartPoint.Start ? time.StartDateTime : time.EndDateTime) +
                                timeOffset.TimeDiff;
                if (!dictionary.ContainsKey(eventTime))
                {
                    dictionary.Add(eventTime, new List<(T, TOffset)>());
                }

                dictionary[eventTime].Add((time, timeOffset));
            }

            return dictionary;
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
