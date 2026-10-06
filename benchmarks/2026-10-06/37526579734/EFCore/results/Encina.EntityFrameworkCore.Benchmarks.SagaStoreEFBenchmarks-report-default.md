
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  99.43 μs | 182.22 μs |  9.988 μs |  1.01 |    0.12 |  10.64 KB |        1.00 |
 AddAsync           |  33.37 μs | 185.71 μs | 10.179 μs |  0.34 |    0.09 |   3.27 KB |        0.31 |
 UpdateAsync        |  48.91 μs | 232.29 μs | 12.733 μs |  0.50 |    0.12 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 762.99 μs | 279.74 μs | 15.334 μs |  7.72 |    0.66 | 335.49 KB |       31.53 |
