```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
| GetAsync           |  95.71 μs | 166.74 μs | 9.140 μs |  1.01 |    0.12 |  10.64 KB |        1.00 |
| AddAsync           |  35.14 μs |  69.16 μs | 3.791 μs |  0.37 |    0.05 |   3.27 KB |        0.31 |
| UpdateAsync        |  39.89 μs | 116.57 μs | 6.390 μs |  0.42 |    0.07 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 714.13 μs | 102.68 μs | 5.628 μs |  7.51 |    0.61 |  335.6 KB |       31.54 |
