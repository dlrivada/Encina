```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  19.04 μs |  1.789 μs | 0.098 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.13 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  27.74 μs | 30.790 μs | 1.688 μs |  1.46 |    0.08 |  1.1597 |      - |  19.57 KB |        1.14 |
| Invalidation_SequentialCommands | 197.54 μs | 20.140 μs | 1.104 μs | 10.37 |    0.07 | 10.4980 | 0.2441 |  171.7 KB |       10.02 |
