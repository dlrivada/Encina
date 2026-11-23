
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |   206.98 μs | 352.37 μs | 19.314 μs |  1.01 |    0.12 |  10.76 KB |        1.00 |
 AddAsync           |    86.89 μs | 305.13 μs | 16.725 μs |  0.42 |    0.08 |   3.27 KB |        0.30 |
 UpdateAsync        |    87.17 μs |  37.44 μs |  2.052 μs |  0.42 |    0.04 |   5.64 KB |        0.52 |
 GetStuckSagasAsync | 1,004.72 μs | 163.46 μs |  8.960 μs |  4.88 |    0.42 | 335.49 KB |       31.19 |
