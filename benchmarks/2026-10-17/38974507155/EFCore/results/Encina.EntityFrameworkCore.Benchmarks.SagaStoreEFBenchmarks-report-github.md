```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 101.17 μs | 543.77 μs | 29.806 μs |  1.05 |    0.36 |  10.64 KB |        1.00 |
| AddAsync           |  34.73 μs |  99.37 μs |  5.447 μs |  0.36 |    0.10 |   3.27 KB |        0.31 |
| UpdateAsync        |  40.19 μs | 164.19 μs |  9.000 μs |  0.42 |    0.13 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 764.32 μs | 452.30 μs | 24.792 μs |  7.96 |    1.81 | 335.49 KB |       31.53 |
