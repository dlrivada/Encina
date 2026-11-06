```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           |   136.30 μs | 232.87 μs | 12.765 μs |  1.01 |    0.11 |  10.64 KB |        1.00 |
| AddAsync           |    54.61 μs | 183.07 μs | 10.034 μs |  0.40 |    0.07 |   3.27 KB |        0.31 |
| UpdateAsync        |    62.55 μs |  86.29 μs |  4.730 μs |  0.46 |    0.05 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 1,001.15 μs | 143.10 μs |  7.844 μs |  7.39 |    0.58 | 335.61 KB |       31.54 |
