
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 764.87 μs | 220.29 μs | 12.075 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |  32.64 μs |  42.83 μs |  2.348 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 102.28 μs | 178.67 μs |  9.793 μs |  0.13 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 100.86 μs | 161.74 μs |  8.865 μs |  0.13 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 464.27 μs | 370.55 μs | 20.311 μs |  0.61 |    0.02 | 166.79 KB |       0.478 |
