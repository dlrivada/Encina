```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  27.62 μs | 17.23 μs | 0.945 μs |  1.00 |    0.04 |  1.0376 | 0.0305 |  17.05 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  42.74 μs | 24.05 μs | 1.318 μs |  1.55 |    0.06 |  1.1597 |      - |   19.4 KB |        1.14 |
| Invalidation_SequentialCommands | 286.26 μs | 19.12 μs | 1.048 μs | 10.37 |    0.30 | 10.2539 |      - |  171.7 KB |       10.07 |
