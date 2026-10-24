```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  21.05 μs |  4.744 μs | 0.260 μs |  1.00 |    0.02 |  1.0376 | 0.0305 |  17.09 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  32.80 μs | 16.416 μs | 0.900 μs |  1.56 |    0.04 |  1.1597 | 0.0610 |  19.42 KB |        1.14 |
| Invalidation_SequentialCommands | 197.86 μs | 93.113 μs | 5.104 μs |  9.40 |    0.23 | 10.4980 | 0.2441 |  171.7 KB |       10.05 |
