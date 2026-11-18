
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  98.99 μs | 119.86 μs |  6.570 μs |  1.00 |    0.08 |  10.64 KB |        1.00 |
 AddAsync           |  32.54 μs |  78.49 μs |  4.303 μs |  0.33 |    0.04 |   3.27 KB |        0.31 |
 UpdateAsync        |  48.04 μs | 120.28 μs |  6.593 μs |  0.49 |    0.06 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 763.55 μs | 370.72 μs | 20.321 μs |  7.74 |    0.46 | 335.49 KB |       31.53 |
