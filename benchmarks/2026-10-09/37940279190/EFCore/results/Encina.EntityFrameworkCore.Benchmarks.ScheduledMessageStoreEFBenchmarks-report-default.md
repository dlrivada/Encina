
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,115.10 μs | 860.79 μs | 47.183 μs |  1.00 |    0.05 | 349.05 KB |       1.000 |
 AddAsync                  |    77.46 μs |  15.50 μs |  0.850 μs |  0.07 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   201.99 μs | 305.05 μs | 16.721 μs |  0.18 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   212.97 μs | 143.56 μs |  7.869 μs |  0.19 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   638.57 μs | 276.78 μs | 15.171 μs |  0.57 |    0.02 | 166.63 KB |       0.477 |
