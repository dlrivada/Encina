
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           |  93.02 μs | 161.27 μs | 8.840 μs |  1.01 |    0.11 |  10.64 KB |        1.00 |
 AddAsync           |  35.01 μs |  93.89 μs | 5.146 μs |  0.38 |    0.06 |   3.27 KB |        0.31 |
 UpdateAsync        |  44.36 μs | 108.77 μs | 5.962 μs |  0.48 |    0.07 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 732.97 μs | 123.19 μs | 6.753 μs |  7.93 |    0.62 | 335.49 KB |       31.53 |
