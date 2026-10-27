```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.70 μs |  4.994 μs | 0.274 μs |  1.00 |    0.01 |  1.0376 |  16.97 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  71.31 μs | 66.283 μs | 3.633 μs |  1.84 |    0.08 |  1.1597 |  19.56 KB |        1.15 |
| Invalidation_SequentialCommands | 410.40 μs | 20.813 μs | 1.141 μs | 10.60 |    0.07 | 10.2539 | 171.71 KB |       10.12 |
