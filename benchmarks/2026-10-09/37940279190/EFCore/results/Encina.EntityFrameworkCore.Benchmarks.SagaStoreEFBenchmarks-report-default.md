
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 124.34 μs | 238.96 μs | 13.098 μs |  1.01 |    0.13 |  10.64 KB |        1.00 |
 AddAsync           |  61.15 μs | 213.13 μs | 11.682 μs |  0.50 |    0.09 |   3.27 KB |        0.31 |
 UpdateAsync        |  63.63 μs |  72.63 μs |  3.981 μs |  0.52 |    0.05 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 992.88 μs | 154.56 μs |  8.472 μs |  8.04 |    0.70 |  335.8 KB |       31.56 |
