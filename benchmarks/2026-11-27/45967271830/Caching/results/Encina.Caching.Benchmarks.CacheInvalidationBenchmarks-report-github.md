```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.15 μs |  5.937 μs | 0.325 μs |  1.00 |    0.01 |  1.0376 |      - |  17.22 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  59.23 μs | 33.971 μs | 1.862 μs |  1.51 |    0.04 |  1.1597 | 0.0610 |  19.48 KB |        1.13 |
| Invalidation_SequentialCommands | 404.80 μs | 58.528 μs | 3.208 μs | 10.34 |    0.10 | 10.2539 |      - | 171.68 KB |        9.97 |
