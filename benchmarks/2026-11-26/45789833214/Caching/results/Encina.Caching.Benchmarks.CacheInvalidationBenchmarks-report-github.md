```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  17.69 μs |  1.333 μs | 0.073 μs |  1.00 |    0.01 |  1.0681 | 0.0305 |  17.45 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  30.64 μs | 23.385 μs | 1.282 μs |  1.73 |    0.06 |  1.1597 | 0.0305 |  19.26 KB |        1.10 |
| Invalidation_SequentialCommands | 191.67 μs | 19.166 μs | 1.051 μs | 10.83 |    0.06 | 10.4980 | 0.2441 |  171.7 KB |        9.84 |
