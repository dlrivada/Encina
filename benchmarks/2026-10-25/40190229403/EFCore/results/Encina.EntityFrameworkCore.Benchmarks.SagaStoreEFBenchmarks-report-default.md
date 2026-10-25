
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 118.10 μs | 354.56 μs | 19.434 μs |  1.02 |    0.20 |  10.64 KB |        1.00 |
 AddAsync           |  42.79 μs | 169.60 μs |  9.296 μs |  0.37 |    0.09 |   3.27 KB |        0.31 |
 UpdateAsync        |  47.58 μs | 166.75 μs |  9.140 μs |  0.41 |    0.09 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 755.87 μs | 492.00 μs | 26.968 μs |  6.51 |    0.89 |  335.8 KB |       31.56 |
