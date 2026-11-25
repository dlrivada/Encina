
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  88.48 μs | 153.98 μs |  8.440 μs |  1.01 |    0.12 |  10.64 KB |        1.00 |
 AddAsync           |  39.15 μs | 124.82 μs |  6.842 μs |  0.45 |    0.08 |   3.27 KB |        0.31 |
 UpdateAsync        |  48.99 μs | 118.11 μs |  6.474 μs |  0.56 |    0.08 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 766.92 μs | 344.31 μs | 18.873 μs |  8.72 |    0.72 | 335.49 KB |       31.53 |
