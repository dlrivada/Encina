```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  36.91 μs |  2.813 μs | 0.154 μs |  1.00 |    0.01 |  1.0986 |      - |  18.23 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  52.84 μs | 15.526 μs | 0.851 μs |  1.43 |    0.02 |  1.1597 | 0.0610 |   19.4 KB |        1.06 |
| Invalidation_SequentialCommands | 423.74 μs | 14.035 μs | 0.769 μs | 11.48 |    0.05 | 10.2539 |      - | 171.71 KB |        9.42 |
