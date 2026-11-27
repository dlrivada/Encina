```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.31 μs |  2.568 μs | 0.141 μs |  1.00 |    0.00 |  1.0376 |  17.12 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  53.09 μs | 22.543 μs | 1.236 μs |  1.35 |    0.03 |  1.1597 |  19.68 KB |        1.15 |
| Invalidation_SequentialCommands | 411.83 μs | 23.507 μs | 1.288 μs | 10.48 |    0.04 | 10.2539 | 171.72 KB |       10.03 |
