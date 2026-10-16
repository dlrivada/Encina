
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  97.67 μs | 339.68 μs | 18.619 μs |  1.02 |    0.23 |  10.64 KB |        1.00 |
 AddAsync           |  37.18 μs |  67.57 μs |  3.704 μs |  0.39 |    0.07 |   3.27 KB |        0.31 |
 UpdateAsync        |  54.16 μs | 238.52 μs | 13.074 μs |  0.57 |    0.15 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 781.78 μs | 705.89 μs | 38.692 μs |  8.18 |    1.28 | 335.49 KB |       31.53 |
