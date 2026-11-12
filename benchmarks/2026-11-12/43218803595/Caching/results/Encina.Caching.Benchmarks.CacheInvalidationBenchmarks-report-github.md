```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  19.38 μs |   5.567 μs | 0.305 μs |  1.00 |    0.02 |  1.0681 | 0.0305 |  17.52 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  32.79 μs |  30.376 μs | 1.665 μs |  1.69 |    0.08 |  1.1902 | 0.0305 |  19.48 KB |        1.11 |
| Invalidation_SequentialCommands | 207.01 μs | 101.096 μs | 5.541 μs | 10.68 |    0.29 | 10.4980 | 0.2441 | 171.72 KB |        9.80 |
