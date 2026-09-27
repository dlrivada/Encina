```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
| GetAsync           | 171.08 μs | 93.78 μs | 5.141 μs |  1.00 |    0.04 |  10.64 KB |        1.00 |
| AddAsync           |  73.35 μs | 31.57 μs | 1.730 μs |  0.43 |    0.01 |   3.27 KB |        0.31 |
| UpdateAsync        |  87.61 μs | 64.65 μs | 3.544 μs |  0.51 |    0.02 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 885.58 μs | 81.44 μs | 4.464 μs |  5.18 |    0.14 |  335.6 KB |       31.54 |
