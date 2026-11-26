```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.91 μs |   1.496 μs | 0.082 μs |  1.00 |    0.00 |  0.9766 |      - |  16.87 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  60.42 μs | 130.521 μs | 7.154 μs |  1.55 |    0.16 |  1.1597 | 0.0610 |  19.38 KB |        1.15 |
| Invalidation_SequentialCommands | 414.36 μs |   7.849 μs | 0.430 μs | 10.65 |    0.02 | 10.2539 |      - |  171.7 KB |       10.18 |
