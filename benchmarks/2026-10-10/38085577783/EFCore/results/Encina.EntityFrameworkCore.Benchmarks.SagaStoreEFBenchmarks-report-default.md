
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean        | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |------------:|-----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           |   187.10 μs | 155.812 μs |  8.541 μs |  1.00 |    0.06 |  10.64 KB |        1.00 |
 AddAsync           |    75.76 μs |   8.322 μs |  0.456 μs |  0.41 |    0.02 |   3.27 KB |        0.31 |
 UpdateAsync        |   100.63 μs | 236.293 μs | 12.952 μs |  0.54 |    0.06 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 1,018.09 μs | 265.820 μs | 14.571 μs |  5.45 |    0.22 | 335.79 KB |       31.56 |
