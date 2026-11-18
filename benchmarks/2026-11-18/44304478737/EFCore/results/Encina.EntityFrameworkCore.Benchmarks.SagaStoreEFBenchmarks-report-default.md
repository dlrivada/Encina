
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|-----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 173.94 μs |  20.682 μs | 1.134 μs |  1.00 |    0.01 |  10.64 KB |        1.00 |
 AddAsync           |  77.05 μs |   8.010 μs | 0.439 μs |  0.44 |    0.00 |   3.27 KB |        0.31 |
 UpdateAsync        |  91.78 μs |  65.094 μs | 3.568 μs |  0.53 |    0.02 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 944.87 μs | 173.735 μs | 9.523 μs |  5.43 |    0.06 | 335.49 KB |       31.53 |
