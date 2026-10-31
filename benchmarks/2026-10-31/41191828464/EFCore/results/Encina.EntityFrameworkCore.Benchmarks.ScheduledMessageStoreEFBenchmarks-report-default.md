
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 770.85 μs | 320.87 μs | 17.588 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
 AddAsync                  |  34.15 μs |  77.61 μs |  4.254 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 106.83 μs | 162.90 μs |  8.929 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 114.22 μs | 328.08 μs | 17.983 μs |  0.15 |    0.02 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 450.23 μs | 275.58 μs | 15.106 μs |  0.58 |    0.02 | 166.55 KB |       0.477 |
