```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.54 μs |  1.758 μs | 0.096 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.25 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  39.45 μs | 21.104 μs | 1.157 μs |  1.49 |    0.04 |  1.1597 |      - |  19.56 KB |        1.13 |
| Invalidation_SequentialCommands | 281.31 μs | 25.511 μs | 1.398 μs | 10.60 |    0.06 | 10.2539 |      - | 171.71 KB |        9.95 |
