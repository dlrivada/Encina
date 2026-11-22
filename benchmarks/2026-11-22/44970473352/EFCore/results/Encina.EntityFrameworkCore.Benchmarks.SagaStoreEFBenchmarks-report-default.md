
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           |  88.28 μs | 1.727 μs | 2.305 μs |  1.00 |    0.04 |  10.64 KB |        1.00 |
 AddAsync           |  29.82 μs | 1.609 μs | 2.148 μs |  0.34 |    0.03 |   3.27 KB |        0.31 |
 UpdateAsync        |  38.41 μs | 1.415 μs | 1.984 μs |  0.44 |    0.02 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 717.43 μs | 4.864 μs | 6.818 μs |  8.13 |    0.22 | 335.49 KB |       31.53 |
