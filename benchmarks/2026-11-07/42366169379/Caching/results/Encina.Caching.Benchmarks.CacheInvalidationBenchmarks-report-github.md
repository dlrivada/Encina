```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.11 μs |   0.858 μs | 0.047 μs |  1.00 |    0.00 |  1.0376 |  17.24 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  54.79 μs |  46.716 μs | 2.561 μs |  1.44 |    0.06 |  1.0986 |  19.25 KB |        1.12 |
| Invalidation_SequentialCommands | 418.03 μs | 150.545 μs | 8.252 μs | 10.97 |    0.19 | 10.2539 |  171.7 KB |        9.96 |
