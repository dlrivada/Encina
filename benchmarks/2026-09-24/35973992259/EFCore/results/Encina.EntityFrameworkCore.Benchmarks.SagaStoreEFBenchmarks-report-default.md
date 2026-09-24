
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 107.61 μs | 313.86 μs | 17.20 μs |  1.02 |    0.19 |  10.64 KB |        1.00 |
 AddAsync           |  41.18 μs | 261.22 μs | 14.32 μs |  0.39 |    0.13 |   3.27 KB |        0.31 |
 UpdateAsync        |  50.54 μs | 307.92 μs | 16.88 μs |  0.48 |    0.15 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 777.73 μs | 220.97 μs | 12.11 μs |  7.34 |    0.95 | 335.49 KB |       31.53 |
