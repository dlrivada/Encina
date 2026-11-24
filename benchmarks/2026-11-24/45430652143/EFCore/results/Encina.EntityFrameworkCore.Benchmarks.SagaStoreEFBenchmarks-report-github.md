```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 102.97 μs | 209.00 μs | 11.456 μs |  1.01 |    0.13 |  10.64 KB |        1.00 |
| AddAsync           |  37.62 μs | 119.27 μs |  6.538 μs |  0.37 |    0.06 |   3.27 KB |        0.31 |
| UpdateAsync        |  44.80 μs | 136.05 μs |  7.457 μs |  0.44 |    0.07 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 868.26 μs | 842.23 μs | 46.165 μs |  8.50 |    0.86 | 335.49 KB |       31.53 |
