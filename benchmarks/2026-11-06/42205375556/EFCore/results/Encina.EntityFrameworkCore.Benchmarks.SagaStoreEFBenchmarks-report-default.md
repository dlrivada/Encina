
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  89.61 μs | 188.74 μs | 10.345 μs |  1.01 |    0.14 |  10.64 KB |        1.00 |
 AddAsync           |  35.05 μs |  66.57 μs |  3.649 μs |  0.39 |    0.05 |   3.27 KB |        0.31 |
 UpdateAsync        |  44.53 μs | 134.17 μs |  7.354 μs |  0.50 |    0.09 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 726.02 μs | 229.95 μs | 12.604 μs |  8.17 |    0.78 | 335.49 KB |       31.53 |
