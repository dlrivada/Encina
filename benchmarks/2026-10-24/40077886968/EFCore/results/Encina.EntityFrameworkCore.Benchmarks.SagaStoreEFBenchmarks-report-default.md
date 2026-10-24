
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 172.37 μs | 48.49 μs | 2.658 μs |  1.00 |    0.02 |  10.64 KB |        1.00 |
 AddAsync           |  70.71 μs | 16.98 μs | 0.931 μs |  0.41 |    0.01 |   3.27 KB |        0.31 |
 UpdateAsync        |  90.02 μs | 45.19 μs | 2.477 μs |  0.52 |    0.01 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 879.34 μs | 81.09 μs | 4.445 μs |  5.10 |    0.07 | 335.49 KB |       31.53 |
