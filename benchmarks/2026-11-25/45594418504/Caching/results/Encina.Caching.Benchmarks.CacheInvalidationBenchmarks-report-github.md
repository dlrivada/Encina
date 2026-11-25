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
| Invalidation_NoMatchingKeys     |  27.01 μs |  1.036 μs | 0.057 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  16.99 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  42.00 μs | 59.440 μs | 3.258 μs |  1.56 |    0.10 |  1.1597 |      - |   19.3 KB |        1.14 |
| Invalidation_SequentialCommands | 280.44 μs | 30.696 μs | 1.683 μs | 10.38 |    0.06 | 10.2539 |      - | 171.71 KB |       10.11 |
