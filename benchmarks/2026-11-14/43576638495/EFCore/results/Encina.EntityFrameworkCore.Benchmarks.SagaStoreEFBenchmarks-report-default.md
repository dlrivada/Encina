
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 107.50 μs | 255.35 μs | 13.997 μs |  1.01 |    0.17 |  10.76 KB |        1.00 |
 AddAsync           |  32.34 μs |  42.65 μs |  2.338 μs |  0.30 |    0.04 |   3.27 KB |        0.30 |
 UpdateAsync        |  53.45 μs | 269.97 μs | 14.798 μs |  0.50 |    0.14 |   5.64 KB |        0.52 |
 GetStuckSagasAsync | 775.41 μs | 378.18 μs | 20.729 μs |  7.30 |    0.90 | 335.49 KB |       31.19 |
