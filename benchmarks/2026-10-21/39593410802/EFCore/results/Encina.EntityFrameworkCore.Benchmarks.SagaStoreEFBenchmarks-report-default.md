
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  91.58 μs | 157.48 μs |  8.632 μs |  1.01 |    0.12 |  10.64 KB |        1.00 |
 AddAsync           |  28.57 μs |  91.59 μs |  5.020 μs |  0.31 |    0.05 |   3.27 KB |        0.31 |
 UpdateAsync        |  42.09 μs | 122.00 μs |  6.687 μs |  0.46 |    0.07 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 711.59 μs | 304.55 μs | 16.693 μs |  7.82 |    0.68 | 335.49 KB |       31.53 |
