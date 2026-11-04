```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  35.21 μs | 33.22 μs | 1.821 μs |  1.00 |    0.06 |  1.0376 |   17.2 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  53.19 μs | 15.69 μs | 0.860 μs |  1.51 |    0.07 |  1.1597 |  19.31 KB |        1.12 |
| Invalidation_SequentialCommands | 359.46 μs | 44.97 μs | 2.465 μs | 10.23 |    0.45 | 10.2539 | 171.71 KB |        9.99 |
