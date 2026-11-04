
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,041.35 μs | 136.67 μs |  7.491 μs |  1.00 |    0.01 | 349.13 KB |       1.000 |
 AddAsync                  |    87.60 μs | 199.47 μs | 10.934 μs |  0.08 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   216.73 μs | 451.76 μs | 24.762 μs |  0.21 |    0.02 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   205.48 μs | 227.51 μs | 12.471 μs |  0.20 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   633.47 μs | 304.46 μs | 16.689 μs |  0.61 |    0.01 | 166.95 KB |       0.478 |
