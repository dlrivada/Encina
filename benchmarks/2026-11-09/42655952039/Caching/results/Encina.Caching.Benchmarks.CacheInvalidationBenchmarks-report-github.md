```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  27.06 μs |  2.184 μs | 0.120 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.02 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  43.36 μs |  6.578 μs | 0.361 μs |  1.60 |    0.01 |  1.1597 |      - |  19.49 KB |        1.15 |
| Invalidation_SequentialCommands | 284.05 μs | 14.119 μs | 0.774 μs | 10.50 |    0.05 | 10.2539 |      - | 171.71 KB |       10.09 |
