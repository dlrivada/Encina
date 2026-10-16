
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 866.60 μs | 254.14 μs | 13.930 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |  38.69 μs |  91.08 μs |  4.993 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 113.89 μs | 201.29 μs | 11.033 μs |  0.13 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 127.13 μs | 320.68 μs | 17.578 μs |  0.15 |    0.02 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 467.65 μs | 270.56 μs | 14.830 μs |  0.54 |    0.02 | 166.55 KB |       0.477 |
