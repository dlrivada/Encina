
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 120.12 μs |  25.00 μs | 1.370 μs |  1.00 |    0.01 |  10.64 KB |        1.00 |
 AddAsync           |  56.10 μs | 161.32 μs | 8.843 μs |  0.47 |    0.06 |   3.27 KB |        0.31 |
 UpdateAsync        |  59.24 μs |  58.98 μs | 3.233 μs |  0.49 |    0.02 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 977.25 μs |  14.26 μs | 0.782 μs |  8.14 |    0.08 |  335.6 KB |       31.54 |
