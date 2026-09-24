
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|----------:|------------:|
 GetDueMessagesAsync       | 1,150.91 μs | 207.63 μs | 11.381 μs |  1.00 | 349.05 KB |       1.000 |
 AddAsync                  |    83.52 μs |  93.71 μs |  5.136 μs |  0.07 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   219.87 μs | 180.80 μs |  9.910 μs |  0.19 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   194.19 μs |  72.49 μs |  3.973 μs |  0.17 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   674.56 μs | 231.21 μs | 12.673 μs |  0.59 | 166.55 KB |       0.477 |
