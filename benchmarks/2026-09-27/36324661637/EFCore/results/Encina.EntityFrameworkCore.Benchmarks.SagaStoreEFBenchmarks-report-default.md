
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 102.13 μs | 432.13 μs | 23.687 μs |  1.03 |    0.28 |  10.64 KB |        1.00 |
 AddAsync           |  39.91 μs |  85.02 μs |  4.660 μs |  0.40 |    0.08 |   3.27 KB |        0.31 |
 UpdateAsync        |  47.70 μs | 129.74 μs |  7.112 μs |  0.48 |    0.11 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 767.16 μs | 353.07 μs | 19.353 μs |  7.76 |    1.39 | 335.49 KB |       31.53 |
