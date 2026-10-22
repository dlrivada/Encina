```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.39 μs | 0.112 μs | 0.164 μs |  1.00 |    0.01 |  1.0376 |  17.09 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  65.05 μs | 3.414 μs | 4.896 μs |  1.65 |    0.12 |  1.0986 |  19.38 KB |        1.13 |
| Invalidation_SequentialCommands | 413.91 μs | 1.169 μs | 1.714 μs | 10.51 |    0.06 | 10.2539 | 171.72 KB |       10.05 |
