```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  25.95 μs |  0.945 μs | 0.052 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.34 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  42.18 μs | 42.796 μs | 2.346 μs |  1.63 |    0.08 |  1.1597 |      - |  19.43 KB |        1.12 |
| Invalidation_SequentialCommands | 278.22 μs | 13.722 μs | 0.752 μs | 10.72 |    0.03 | 10.2539 |      - | 171.72 KB |        9.90 |
