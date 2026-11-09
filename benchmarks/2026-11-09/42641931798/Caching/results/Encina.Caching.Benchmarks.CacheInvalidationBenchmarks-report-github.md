```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.93 μs |  5.274 μs | 0.289 μs |  1.00 |    0.01 |  1.0376 |  17.15 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  57.76 μs | 12.978 μs | 0.711 μs |  1.45 |    0.02 |  1.1597 |   19.7 KB |        1.15 |
| Invalidation_SequentialCommands | 420.04 μs | 22.421 μs | 1.229 μs | 10.52 |    0.07 | 10.2539 | 171.71 KB |       10.01 |
