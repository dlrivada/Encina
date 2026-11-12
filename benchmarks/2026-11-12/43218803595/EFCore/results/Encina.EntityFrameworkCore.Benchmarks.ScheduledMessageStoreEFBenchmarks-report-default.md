
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
-------------------------- |------------:|----------:|----------:|------:|----------:|------------:|
 GetDueMessagesAsync       | 1,072.23 μs | 120.69 μs |  6.616 μs |  1.00 | 349.28 KB |       1.000 |
 AddAsync                  |    52.58 μs |  14.10 μs |  0.773 μs |  0.05 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   144.54 μs |  27.51 μs |  1.508 μs |  0.13 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   156.22 μs | 302.65 μs | 16.589 μs |  0.15 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   600.33 μs | 115.90 μs |  6.353 μs |  0.56 | 166.55 KB |       0.477 |
