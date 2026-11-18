```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  25.97 μs |  0.393 μs | 0.022 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.24 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  40.10 μs |  1.277 μs | 0.070 μs |  1.54 |    0.00 |  1.1597 |      - |   19.4 KB |        1.13 |
| Invalidation_SequentialCommands | 277.89 μs | 26.391 μs | 1.447 μs | 10.70 |    0.05 | 10.2539 |      - | 171.71 KB |        9.96 |
