
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error       | StdDev     | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|------------:|-----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 829.06 μs |   293.98 μs |  16.114 μs | 823.27 μs |  1.00 |    0.02 | 349.13 KB |       1.000 |
 AddAsync                  |  35.71 μs |   113.05 μs |   6.197 μs |  32.69 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 171.62 μs |   952.62 μs |  52.216 μs | 144.26 μs |  0.21 |    0.05 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 167.02 μs | 1,952.78 μs | 107.038 μs | 115.50 μs |  0.20 |    0.11 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 456.80 μs |   425.78 μs |  23.339 μs | 444.41 μs |  0.55 |    0.03 | 166.55 KB |       0.477 |
