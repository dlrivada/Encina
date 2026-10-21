
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 102.14 μs | 230.35 μs | 12.626 μs |  1.01 |    0.15 |  10.64 KB |        1.00 |
 AddAsync           |  35.15 μs |  76.07 μs |  4.169 μs |  0.35 |    0.05 |   3.27 KB |        0.31 |
 UpdateAsync        |  50.88 μs | 123.09 μs |  6.747 μs |  0.50 |    0.08 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 779.17 μs | 486.28 μs | 26.655 μs |  7.70 |    0.81 | 335.49 KB |       31.53 |
