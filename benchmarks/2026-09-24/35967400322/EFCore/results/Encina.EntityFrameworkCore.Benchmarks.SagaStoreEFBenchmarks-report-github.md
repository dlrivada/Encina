```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 191.62 μs | 510.11 μs | 27.961 μs |  1.01 |    0.18 |  10.64 KB |        1.00 |
| AddAsync           |  73.84 μs |  67.44 μs |  3.697 μs |  0.39 |    0.05 |   3.27 KB |        0.31 |
| UpdateAsync        |  89.24 μs |  27.80 μs |  1.524 μs |  0.47 |    0.06 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 875.47 μs | 453.47 μs | 24.856 μs |  4.63 |    0.56 | 335.49 KB |       31.53 |
