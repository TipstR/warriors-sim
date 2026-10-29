using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WarriorsSim.BalanceRunner;
using WarriorsSim.Simulation;

// Runs many seeds of ThunderClan for many years and prints balance statistics.
// Usage (in the project folder, Release is much faster):
//   dotnet run -c Release --project tools/BalanceRunner -- [seeds] [years] [Name=value ...]
// Name=value changes a Tuning value for this run, e.g. HuntingPracticePerMouse=0.004.
// Policy=Compassionate picks a leader style (see FeedingPolicy.Presets).
// Defaults: 100 seeds × 30 years. 10 seeds are too noisy (±4 die-outs per 100 is normal).

// Always "0.5", never "0,5", whatever the computer's language
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

var seeds = args.Length > 0 ? int.Parse(args[0]) : 100;
var years = args.Length > 1 ? int.Parse(args[1]) : 30;
foreach (var setting in args.Skip(2)) {
    var parts = setting.Split('=');
    if (parts[0] == "Policy")
        Tuning.DefaultFeedingPolicy = FeedingPolicy.Presets[parts[1]];
    else
        TuningSettings.Get(parts[0]).Value = parts[1];
    Console.WriteLine($"{parts[0]} = {parts[1]}");
}

var stopwatch = Stopwatch.StartNew();
var results = new ConcurrentBag<SeedResult>();
// Seeds run at the same time. That is safe because Tuning is only read during a run.
Parallel.For(1, seeds + 1, seed => results.Add(SeedRun.Run(seed, years)));
Report.Print(results.ToList(), seeds, years, stopwatch.Elapsed);
