
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 788.73 μs | 275.51 μs | 15.102 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |  33.36 μs | 116.99 μs |  6.412 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 124.53 μs | 331.23 μs | 18.156 μs |  0.16 |    0.02 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 108.42 μs | 197.61 μs | 10.832 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 457.84 μs | 610.27 μs | 33.451 μs |  0.58 |    0.04 | 166.55 KB |       0.477 |
