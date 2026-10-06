```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 118.28 μs |  45.12 μs |  2.473 μs |  1.00 |    0.03 |  10.64 KB |        1.00 |
| AddAsync           |  56.35 μs |  31.84 μs |  1.745 μs |  0.48 |    0.02 |   3.27 KB |        0.31 |
| UpdateAsync        |  67.48 μs | 307.15 μs | 16.836 μs |  0.57 |    0.12 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 975.52 μs | 114.28 μs |  6.264 μs |  8.25 |    0.15 | 335.49 KB |       31.53 |
