```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.14 μs |   3.444 μs |  0.189 μs |  1.00 |    0.01 |  0.9766 |   16.9 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  71.64 μs | 197.664 μs | 10.835 μs |  1.83 |    0.24 |  1.0986 |  19.39 KB |        1.15 |
| Invalidation_SequentialCommands | 407.32 μs |  30.279 μs |  1.660 μs | 10.41 |    0.06 | 10.2539 | 171.71 KB |       10.16 |
