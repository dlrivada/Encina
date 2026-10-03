
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 818.38 μs | 276.44 μs | 15.153 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |  38.10 μs |  69.73 μs |  3.822 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 116.02 μs | 224.78 μs | 12.321 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 132.03 μs | 209.74 μs | 11.496 μs |  0.16 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 454.81 μs | 472.20 μs | 25.883 μs |  0.56 |    0.03 | 166.67 KB |       0.478 |
