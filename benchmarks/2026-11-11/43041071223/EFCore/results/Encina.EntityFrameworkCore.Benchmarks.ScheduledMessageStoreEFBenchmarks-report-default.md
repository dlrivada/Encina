
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 827.31 μs | 260.70 μs | 14.290 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |  36.02 μs |  87.78 μs |  4.812 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 108.98 μs | 320.14 μs | 17.548 μs |  0.13 |    0.02 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 117.84 μs | 196.91 μs | 10.793 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 466.66 μs | 545.94 μs | 29.925 μs |  0.56 |    0.03 | 166.66 KB |       0.477 |
