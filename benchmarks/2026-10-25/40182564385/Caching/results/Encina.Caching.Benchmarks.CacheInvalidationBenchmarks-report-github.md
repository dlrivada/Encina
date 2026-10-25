```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  19.34 μs | 18.184 μs | 0.997 μs |  1.00 |    0.06 |  1.0376 | 0.0305 |   17.4 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  32.36 μs |  6.842 μs | 0.375 μs |  1.68 |    0.07 |  1.1597 |      - |  19.42 KB |        1.12 |
| Invalidation_SequentialCommands | 199.58 μs | 20.187 μs | 1.107 μs | 10.34 |    0.45 | 10.4980 | 0.2441 |  171.7 KB |        9.87 |
