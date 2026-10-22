```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.26 μs |  4.716 μs | 0.259 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.37 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  44.39 μs | 43.493 μs | 2.384 μs |  1.69 |    0.08 |  1.1597 | 0.0610 |  19.55 KB |        1.13 |
| Invalidation_SequentialCommands | 278.95 μs | 12.152 μs | 0.666 μs | 10.62 |    0.09 | 10.2539 |      - | 171.71 KB |        9.89 |
