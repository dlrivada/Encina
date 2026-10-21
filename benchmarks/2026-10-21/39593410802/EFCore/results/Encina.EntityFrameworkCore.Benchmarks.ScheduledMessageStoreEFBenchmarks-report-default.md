
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 768.24 μs | 414.00 μs | 22.693 μs |  1.00 |    0.04 | 349.05 KB |       1.000 |
 AddAsync                  |  37.14 μs | 152.95 μs |  8.384 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 117.56 μs | 291.03 μs | 15.952 μs |  0.15 |    0.02 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 104.33 μs | 154.96 μs |  8.494 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 455.55 μs | 362.70 μs | 19.881 μs |  0.59 |    0.03 | 167.02 KB |       0.479 |
