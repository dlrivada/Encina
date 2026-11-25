```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  18.86 μs |  5.763 μs | 0.316 μs |  1.00 |    0.02 |  1.0681 | 0.0305 |  17.45 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  40.21 μs | 45.191 μs | 2.477 μs |  2.13 |    0.12 |  1.1597 | 0.6714 |   19.6 KB |        1.12 |
| Invalidation_SequentialCommands | 202.98 μs | 55.239 μs | 3.028 μs | 10.76 |    0.21 | 10.4980 | 0.2441 |  171.7 KB |        9.84 |
