```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.57 μs |  1.994 μs | 0.109 μs |  1.00 |    0.00 |  1.0376 |  17.01 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  54.43 μs | 25.179 μs | 1.380 μs |  1.38 |    0.03 |  1.1597 |  19.44 KB |        1.14 |
| Invalidation_SequentialCommands | 417.61 μs | 21.595 μs | 1.184 μs | 10.55 |    0.04 | 10.2539 | 171.71 KB |       10.10 |
