
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,001.28 μs | 167.89 μs |  9.203 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
 AddAsync                  |    78.05 μs |  29.63 μs |  1.624 μs |  0.08 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   205.06 μs | 212.28 μs | 11.636 μs |  0.20 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   201.97 μs | 451.79 μs | 24.764 μs |  0.20 |    0.02 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   584.90 μs | 174.72 μs |  9.577 μs |  0.58 |    0.01 | 166.55 KB |       0.477 |
