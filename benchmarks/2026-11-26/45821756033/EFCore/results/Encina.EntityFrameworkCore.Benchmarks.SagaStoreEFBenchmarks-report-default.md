
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 121.52 μs | 402.33 μs | 22.053 μs |  1.03 |    0.24 |  10.64 KB |        1.00 |
 AddAsync           |  29.70 μs |  90.38 μs |  4.954 μs |  0.25 |    0.06 |   3.27 KB |        0.31 |
 UpdateAsync        |  46.45 μs | 116.31 μs |  6.375 μs |  0.39 |    0.08 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 747.38 μs | 483.79 μs | 26.518 μs |  6.30 |    1.12 | 335.49 KB |       31.53 |
