```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 196.50 μs | 246.40 μs | 13.506 μs |  1.00 |    0.08 |  10.64 KB |        1.00 |
| AddAsync           |  80.97 μs |  57.18 μs |  3.134 μs |  0.41 |    0.03 |   3.27 KB |        0.31 |
| UpdateAsync        |  97.06 μs | 245.19 μs | 13.440 μs |  0.50 |    0.07 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 921.54 μs | 173.17 μs |  9.492 μs |  4.70 |    0.28 | 335.49 KB |       31.53 |
