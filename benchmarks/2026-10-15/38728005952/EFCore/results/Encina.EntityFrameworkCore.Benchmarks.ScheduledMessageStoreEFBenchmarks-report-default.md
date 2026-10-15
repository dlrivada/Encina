
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 808.50 μs | 255.45 μs | 14.002 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |  39.38 μs |  87.95 μs |  4.821 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 114.79 μs | 321.29 μs | 17.611 μs |  0.14 |    0.02 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 106.48 μs | 207.82 μs | 11.391 μs |  0.13 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 467.49 μs | 671.17 μs | 36.789 μs |  0.58 |    0.04 | 166.55 KB |       0.477 |
