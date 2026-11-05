```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.56 μs |  2.054 μs | 0.113 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.24 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  42.54 μs | 50.671 μs | 2.777 μs |  1.60 |    0.09 |  1.1597 |      - |  19.53 KB |        1.13 |
| Invalidation_SequentialCommands | 288.87 μs | 64.049 μs | 3.511 μs | 10.88 |    0.12 | 10.2539 |      - |  171.7 KB |        9.96 |
