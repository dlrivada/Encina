
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method             | Mean      | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|---------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |  85.33 μs | 3.766 μs |  5.155 μs |  1.00 |    0.08 |  10.64 KB |        1.00 |
 AddAsync           |  26.90 μs | 1.663 μs |  2.386 μs |  0.32 |    0.03 |   3.27 KB |        0.31 |
 UpdateAsync        |  36.64 μs | 2.465 μs |  3.456 μs |  0.43 |    0.05 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 723.75 μs | 8.590 μs | 12.857 μs |  8.51 |    0.52 | 335.49 KB |       31.53 |
