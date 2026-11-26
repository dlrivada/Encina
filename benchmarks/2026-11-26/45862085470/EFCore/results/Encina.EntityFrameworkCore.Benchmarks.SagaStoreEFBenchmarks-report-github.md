```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           |  92.97 μs | 299.69 μs | 16.427 μs |  1.02 |    0.21 |  10.64 KB |        1.00 |
| AddAsync           |  35.91 μs |  72.76 μs |  3.988 μs |  0.39 |    0.07 |   3.27 KB |        0.31 |
| UpdateAsync        |  49.45 μs | 266.46 μs | 14.606 μs |  0.54 |    0.16 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 724.83 μs | 400.83 μs | 21.971 μs |  7.95 |    1.12 | 335.49 KB |       31.53 |
