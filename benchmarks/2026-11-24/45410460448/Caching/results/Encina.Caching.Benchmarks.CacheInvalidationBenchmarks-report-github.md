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
| Invalidation_NoMatchingKeys     |  27.00 μs |  7.999 μs | 0.438 μs |  1.00 |    0.02 |  1.0376 | 0.0305 |  17.26 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  40.20 μs | 28.704 μs | 1.573 μs |  1.49 |    0.05 |  1.1597 |      - |  19.54 KB |        1.13 |
| Invalidation_SequentialCommands | 287.22 μs | 54.282 μs | 2.975 μs | 10.64 |    0.18 | 10.2539 |      - | 171.69 KB |        9.95 |
