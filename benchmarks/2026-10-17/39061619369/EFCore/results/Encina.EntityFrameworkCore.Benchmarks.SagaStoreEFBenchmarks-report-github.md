```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           |  97.25 μs | 356.14 μs | 19.521 μs |  1.02 |    0.24 |  10.64 KB |        1.00 |
| AddAsync           |  35.03 μs |  95.90 μs |  5.257 μs |  0.37 |    0.08 |   3.27 KB |        0.31 |
| UpdateAsync        |  46.48 μs | 100.16 μs |  5.490 μs |  0.49 |    0.09 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 743.76 μs | 213.87 μs | 11.723 μs |  7.84 |    1.23 | 335.49 KB |       31.53 |
