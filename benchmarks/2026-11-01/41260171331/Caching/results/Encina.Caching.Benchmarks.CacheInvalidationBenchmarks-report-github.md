```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.61 μs | 0.217 μs | 0.304 μs |  1.00 |    0.01 |  1.0376 |   17.1 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  60.56 μs | 4.989 μs | 7.467 μs |  1.53 |    0.19 |  1.1597 |  19.65 KB |        1.15 |
| Invalidation_SequentialCommands | 422.63 μs | 0.931 μs | 1.394 μs | 10.67 |    0.09 | 10.2539 |  171.7 KB |       10.04 |
