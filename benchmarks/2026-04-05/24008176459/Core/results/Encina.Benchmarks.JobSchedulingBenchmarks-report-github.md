```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                         | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Quartz (Immediate Trigger)&#39;   |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Quartz (Delayed Trigger +5s)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Quartz (CRON Daily Trigger)&#39;  |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  JobSchedulingBenchmarks.'Quartz (Immediate Trigger)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  JobSchedulingBenchmarks.'Quartz (Delayed Trigger +5s)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  JobSchedulingBenchmarks.'Quartz (CRON Daily Trigger)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
