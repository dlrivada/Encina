```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           |  92.76 μs | 168.30 μs |  9.225 μs |  1.01 |    0.12 |  10.64 KB |        1.00 |
| AddAsync           |  37.18 μs |  52.36 μs |  2.870 μs |  0.40 |    0.04 |   3.27 KB |        0.31 |
| UpdateAsync        |  47.56 μs | 140.73 μs |  7.714 μs |  0.52 |    0.08 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 728.37 μs | 300.24 μs | 16.457 μs |  7.90 |    0.67 | 335.49 KB |       31.53 |
