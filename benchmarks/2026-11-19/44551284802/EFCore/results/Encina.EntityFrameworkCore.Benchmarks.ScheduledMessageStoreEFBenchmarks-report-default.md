
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,124.38 μs | 517.12 μs | 28.345 μs |  1.00 |    0.03 | 349.27 KB |       1.000 |
 AddAsync                  |    49.49 μs |  15.19 μs |  0.833 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   145.23 μs | 101.59 μs |  5.569 μs |  0.13 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   146.68 μs |  39.81 μs |  2.182 μs |  0.13 |    0.00 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   655.65 μs | 497.05 μs | 27.245 μs |  0.58 |    0.02 | 166.67 KB |       0.477 |
