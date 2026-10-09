
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 185.40 μs | 741.99 μs | 40.67 μs |  1.03 |    0.27 |  10.64 KB |        1.00 |
 AddAsync           |  51.26 μs | 234.83 μs | 12.87 μs |  0.28 |    0.08 |   3.27 KB |        0.31 |
 UpdateAsync        |  53.73 μs | 235.78 μs | 12.92 μs |  0.30 |    0.08 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 918.84 μs | 913.77 μs | 50.09 μs |  5.11 |    0.94 | 335.49 KB |       31.53 |
