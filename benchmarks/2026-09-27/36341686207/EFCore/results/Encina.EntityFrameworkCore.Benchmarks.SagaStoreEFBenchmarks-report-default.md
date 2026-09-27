
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |   173.94 μs | 625.91 μs | 34.308 μs |  1.02 |    0.24 |  10.64 KB |        1.00 |
 AddAsync           |    77.21 μs | 133.61 μs |  7.323 μs |  0.45 |    0.08 |   3.27 KB |        0.31 |
 UpdateAsync        |    71.75 μs | 165.74 μs |  9.085 μs |  0.42 |    0.08 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 1,074.29 μs | 556.33 μs | 30.495 μs |  6.32 |    0.99 | 335.49 KB |       31.53 |
