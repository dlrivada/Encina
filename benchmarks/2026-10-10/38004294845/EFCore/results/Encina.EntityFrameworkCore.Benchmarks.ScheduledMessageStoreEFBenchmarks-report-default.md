
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error       | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|------------:|---------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 862.12 μs |   464.52 μs | 25.46 μs |  1.00 |    0.04 | 349.47 KB |       1.000 |
 AddAsync                  |  55.40 μs |   207.28 μs | 11.36 μs |  0.06 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 233.19 μs |   511.11 μs | 28.02 μs |  0.27 |    0.03 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 157.96 μs |   776.67 μs | 42.57 μs |  0.18 |    0.04 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 632.76 μs | 1,035.52 μs | 56.76 μs |  0.73 |    0.06 | 166.55 KB |       0.477 |
