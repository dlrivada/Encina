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
| Invalidation_NoMatchingKeys     |  26.75 μs |  0.494 μs | 0.027 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.35 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  42.25 μs | 39.576 μs | 2.169 μs |  1.58 |    0.07 |  1.1597 |      - |  19.59 KB |        1.13 |
| Invalidation_SequentialCommands | 293.01 μs | 21.792 μs | 1.194 μs | 10.95 |    0.04 | 10.2539 |      - | 171.71 KB |        9.90 |
