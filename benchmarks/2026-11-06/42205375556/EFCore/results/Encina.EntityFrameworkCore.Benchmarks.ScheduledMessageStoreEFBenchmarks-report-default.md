
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 775.61 μs | 145.59 μs |  7.980 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
 AddAsync                  |  36.46 μs | 120.52 μs |  6.606 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 105.95 μs | 194.73 μs | 10.674 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 105.29 μs | 150.31 μs |  8.239 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 477.11 μs | 349.06 μs | 19.133 μs |  0.62 |    0.02 | 166.55 KB |       0.477 |
