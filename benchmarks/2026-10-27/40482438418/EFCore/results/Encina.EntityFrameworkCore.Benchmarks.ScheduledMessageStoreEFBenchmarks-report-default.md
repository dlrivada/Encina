
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 835.27 μs | 407.27 μs | 22.324 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
 AddAsync                  |  35.58 μs |  84.13 μs |  4.611 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 103.95 μs | 232.86 μs | 12.764 μs |  0.12 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 123.19 μs | 210.63 μs | 11.545 μs |  0.15 |    0.01 |  13.33 KB |       0.038 |
 GetRecurringMessagesAsync | 449.63 μs | 384.84 μs | 21.094 μs |  0.54 |    0.03 | 166.55 KB |       0.477 |
