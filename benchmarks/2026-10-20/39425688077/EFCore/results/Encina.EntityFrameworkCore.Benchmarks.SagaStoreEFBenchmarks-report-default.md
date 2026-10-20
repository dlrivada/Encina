
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 103.19 μs | 564.14 μs | 30.922 μs |  1.05 |    0.37 |  10.64 KB |        1.00 |
 AddAsync           |  34.71 μs | 115.31 μs |  6.320 μs |  0.35 |    0.10 |   3.27 KB |        0.31 |
 UpdateAsync        |  41.34 μs | 179.31 μs |  9.829 μs |  0.42 |    0.13 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 883.35 μs | 858.07 μs | 47.034 μs |  9.02 |    2.06 | 335.49 KB |       31.53 |
