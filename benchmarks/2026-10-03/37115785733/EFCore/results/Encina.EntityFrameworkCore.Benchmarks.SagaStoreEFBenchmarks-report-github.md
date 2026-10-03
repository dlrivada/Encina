```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
| GetAsync           | 117.77 μs | 120.45 μs | 6.602 μs |  1.00 |    0.07 |  10.64 KB |        1.00 |
| AddAsync           |  50.56 μs |  32.54 μs | 1.784 μs |  0.43 |    0.02 |   3.27 KB |        0.31 |
| UpdateAsync        |  66.17 μs |  57.38 μs | 3.145 μs |  0.56 |    0.04 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 993.86 μs |  34.15 μs | 1.872 μs |  8.46 |    0.40 | 335.49 KB |       31.53 |
