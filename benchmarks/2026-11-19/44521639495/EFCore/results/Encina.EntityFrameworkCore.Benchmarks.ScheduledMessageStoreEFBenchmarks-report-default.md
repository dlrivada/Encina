
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|----------:|------------:|
 GetDueMessagesAsync       | 1,064.48 μs | 194.86 μs | 10.681 μs |  1.00 | 349.05 KB |       1.000 |
 AddAsync                  |    53.51 μs | 142.91 μs |  7.833 μs |  0.05 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   139.79 μs |  89.89 μs |  4.927 μs |  0.13 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   146.79 μs | 115.78 μs |  6.346 μs |  0.14 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   623.49 μs | 305.42 μs | 16.741 μs |  0.59 | 166.55 KB |       0.477 |
