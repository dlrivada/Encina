
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,266.81 μs | 477.23 μs | 26.159 μs |  1.00 |    0.03 |  349.2 KB |       1.000 |
 AddAsync                  |    71.03 μs | 100.10 μs |  5.487 μs |  0.06 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   191.18 μs | 211.17 μs | 11.575 μs |  0.15 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   183.20 μs | 456.50 μs | 25.022 μs |  0.14 |    0.02 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   675.88 μs | 116.55 μs |  6.388 μs |  0.53 |    0.01 | 166.55 KB |       0.477 |
