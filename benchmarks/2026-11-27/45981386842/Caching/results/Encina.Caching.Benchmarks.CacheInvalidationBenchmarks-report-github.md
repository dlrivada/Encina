```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.69 μs |  1.378 μs | 0.076 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.05 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  39.20 μs | 15.959 μs | 0.875 μs |  1.47 |    0.03 |  1.1597 | 0.0610 |  19.54 KB |        1.15 |
| Invalidation_SequentialCommands | 279.12 μs | 64.174 μs | 3.518 μs | 10.46 |    0.12 | 10.2539 |      - | 171.71 KB |       10.07 |
