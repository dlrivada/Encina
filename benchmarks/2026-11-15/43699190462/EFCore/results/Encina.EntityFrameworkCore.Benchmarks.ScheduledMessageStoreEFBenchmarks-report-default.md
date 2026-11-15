
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 771.45 μs | 361.08 μs | 19.792 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
 AddAsync                  |  35.95 μs |  72.06 μs |  3.950 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  | 108.68 μs | 166.83 μs |  9.144 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         | 106.70 μs | 150.77 μs |  8.264 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync | 452.01 μs | 140.11 μs |  7.680 μs |  0.59 |    0.02 | 166.55 KB |       0.477 |
