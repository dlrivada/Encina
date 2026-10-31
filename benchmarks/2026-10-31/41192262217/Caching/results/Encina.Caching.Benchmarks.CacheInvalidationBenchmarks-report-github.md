```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.99 μs |  1.726 μs | 0.095 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.14 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  40.32 μs | 33.084 μs | 1.813 μs |  1.49 |    0.06 |  1.1597 |      - |  19.74 KB |        1.15 |
| Invalidation_SequentialCommands | 283.87 μs |  4.036 μs | 0.221 μs | 10.52 |    0.03 | 10.2539 |      - | 171.71 KB |       10.02 |
