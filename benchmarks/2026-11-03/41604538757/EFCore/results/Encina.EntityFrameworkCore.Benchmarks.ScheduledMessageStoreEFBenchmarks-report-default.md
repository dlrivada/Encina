
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 837.03 μs | 321.91 μs | 17.645 μs |  1.00 |    0.03 | 349.34 KB |       1.000 |
 AddAsync                  |  40.56 μs |  93.88 μs |  5.146 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 144.58 μs | 585.31 μs | 32.083 μs |  0.17 |    0.03 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 124.89 μs | 498.57 μs | 27.328 μs |  0.15 |    0.03 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 506.18 μs | 437.59 μs | 23.986 μs |  0.60 |    0.03 | 166.55 KB |       0.477 |
