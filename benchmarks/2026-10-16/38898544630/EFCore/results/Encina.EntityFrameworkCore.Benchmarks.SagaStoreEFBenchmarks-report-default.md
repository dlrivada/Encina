
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 120.17 μs | 670.65 μs | 36.76 μs |  1.06 |    0.37 |  10.64 KB |        1.00 |
 AddAsync           |  43.07 μs | 192.02 μs | 10.53 μs |  0.38 |    0.12 |   3.27 KB |        0.31 |
 UpdateAsync        |  43.55 μs | 185.96 μs | 10.19 μs |  0.38 |    0.12 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 749.71 μs | 275.97 μs | 15.13 μs |  6.59 |    1.49 | 335.49 KB |       31.53 |
