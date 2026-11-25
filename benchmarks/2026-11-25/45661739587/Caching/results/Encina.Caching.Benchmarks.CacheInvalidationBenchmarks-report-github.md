```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  25.77 μs |  3.312 μs | 0.182 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.42 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  43.69 μs |  3.897 μs | 0.214 μs |  1.70 |    0.01 |  1.1597 |      - |  19.46 KB |        1.12 |
| Invalidation_SequentialCommands | 283.45 μs | 38.894 μs | 2.132 μs | 11.00 |    0.10 | 10.2539 |      - |  171.7 KB |        9.86 |
