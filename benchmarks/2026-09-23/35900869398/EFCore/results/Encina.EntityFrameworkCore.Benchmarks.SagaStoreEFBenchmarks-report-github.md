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
| GetAsync           | 178.00 μs | 286.67 μs | 15.713 μs |  1.01 |    0.11 |  10.64 KB |        1.00 |
| AddAsync           |  74.48 μs |  40.57 μs |  2.224 μs |  0.42 |    0.03 |   3.27 KB |        0.31 |
| UpdateAsync        |  84.24 μs |  42.64 μs |  2.337 μs |  0.48 |    0.04 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 872.16 μs |  51.28 μs |  2.811 μs |  4.92 |    0.37 | 335.49 KB |       31.53 |
