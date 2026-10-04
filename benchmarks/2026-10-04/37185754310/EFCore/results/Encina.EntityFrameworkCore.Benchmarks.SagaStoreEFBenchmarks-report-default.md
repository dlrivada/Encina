
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 121.45 μs |  32.87 μs | 1.802 μs |  1.00 |    0.02 |  10.64 KB |        1.00 |
 AddAsync           |  50.23 μs |  46.08 μs | 2.526 μs |  0.41 |    0.02 |   3.27 KB |        0.31 |
 UpdateAsync        |  62.87 μs | 145.01 μs | 7.948 μs |  0.52 |    0.06 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 970.92 μs | 138.71 μs | 7.603 μs |  8.00 |    0.12 | 335.49 KB |       31.53 |
