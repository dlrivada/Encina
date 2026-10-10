
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|---------:|------:|----------:|------------:|
 GetDueMessagesAsync       | 1,053.72 μs | 155.24 μs | 8.509 μs |  1.00 | 349.28 KB |       1.000 |
 AddAsync                  |    51.62 μs |  21.72 μs | 1.191 μs |  0.05 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   140.61 μs |  79.91 μs | 4.380 μs |  0.13 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   140.03 μs | 181.96 μs | 9.974 μs |  0.13 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   601.11 μs |  35.70 μs | 1.957 μs |  0.57 | 166.55 KB |       0.477 |
