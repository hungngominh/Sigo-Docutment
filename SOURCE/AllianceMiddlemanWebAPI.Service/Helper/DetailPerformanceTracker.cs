using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AllianceMiddlemanWebAPI.Shared.Helper
{
    /// <summary>
    /// Lightweight per-step performance tracker for the Vehicle Detail flow.
    /// Usage:
    ///   var tracker = new DetailPerformanceTracker(vehicleId);
    ///   tracker.Start("StepName");
    ///   // ... do work ...
    ///   tracker.Stop("StepName");
    ///   tracker.LogResult();
    /// </summary>
    public class DetailPerformanceTracker
    {
        private readonly string _vehicleId;
        private readonly Stopwatch _totalStopwatch;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Stopwatch> _stepTimers = new();
        private readonly List<string> _stepOrder = new();

        public DetailPerformanceTracker(string vehicleId)
        {
            _vehicleId = vehicleId;
            _totalStopwatch = Stopwatch.StartNew();
        }

        public void Start(string stepName)
        {
            var sw = _stepTimers.GetOrAdd(stepName, _ =>
            {
                lock (_stepOrder) { _stepOrder.Add(stepName); }
                return new Stopwatch();
            });
            sw.Start();
        }

        public void Stop(string stepName)
        {
            if (_stepTimers.TryGetValue(stepName, out var sw))
                sw.Stop();
        }

        public long GetStepMs(string stepName)
        {
            return _stepTimers.TryGetValue(stepName, out var sw) ? sw.ElapsedMilliseconds : -1;
        }

        public string GetLogOutput()
        {
            _totalStopwatch.Stop();
            var sb = new StringBuilder();
            sb.AppendLine($"[DetailPerf] VehicleId={_vehicleId} | Total={_totalStopwatch.ElapsedMilliseconds}ms");
            foreach (var step in _stepOrder)
            {
                var ms = _stepTimers[step].ElapsedMilliseconds;
                sb.AppendLine($"  {step}: {ms}ms");
            }
            return sb.ToString();
        }

        public Dictionary<string, long> GetStepResults()
        {
            return _stepOrder.ToDictionary(s => s, s => _stepTimers[s].ElapsedMilliseconds);
        }

        public long TotalMs => _totalStopwatch.ElapsedMilliseconds;
    }
}
