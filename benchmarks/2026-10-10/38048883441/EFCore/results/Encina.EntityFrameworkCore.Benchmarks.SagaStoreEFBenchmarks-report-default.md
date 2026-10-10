
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  97.11 μs | 514.49 μs | 28.201 μs |  1.05 |    0.35 |  10.64 KB |        1.00 |
 AddAsync           |  37.06 μs | 116.55 μs |  6.388 μs |  0.40 |    0.11 |   3.27 KB |        0.31 |
 UpdateAsync        |  39.78 μs | 144.21 μs |  7.905 μs |  0.43 |    0.12 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 721.89 μs | 299.17 μs | 16.398 μs |  7.81 |    1.70 | 335.64 KB |       31.54 |
