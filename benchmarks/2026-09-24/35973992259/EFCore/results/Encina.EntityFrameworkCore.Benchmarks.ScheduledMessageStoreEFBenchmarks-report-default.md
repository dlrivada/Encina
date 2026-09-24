
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,145.07 μs | 570.41 μs | 31.266 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
 AddAsync                  |    75.46 μs |  40.68 μs |  2.230 μs |  0.07 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   197.99 μs | 188.60 μs | 10.338 μs |  0.17 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   184.75 μs |  14.87 μs |  0.815 μs |  0.16 |    0.00 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   582.24 μs | 419.78 μs | 23.009 μs |  0.51 |    0.02 | 166.55 KB |       0.477 |
