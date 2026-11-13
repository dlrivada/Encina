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
| Invalidation_NoMatchingKeys     |  26.73 μs |  2.505 μs | 0.137 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.17 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  41.46 μs | 38.482 μs | 2.109 μs |  1.55 |    0.07 |  1.1597 |      - |  19.55 KB |        1.14 |
| Invalidation_SequentialCommands | 283.00 μs | 40.705 μs | 2.231 μs | 10.59 |    0.09 | 10.2539 |      - | 171.72 KB |       10.00 |
