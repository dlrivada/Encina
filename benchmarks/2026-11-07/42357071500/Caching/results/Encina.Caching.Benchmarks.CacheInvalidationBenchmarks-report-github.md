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
| Invalidation_NoMatchingKeys     |  26.60 μs |  4.196 μs | 0.230 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.27 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  42.75 μs | 40.745 μs | 2.233 μs |  1.61 |    0.07 |  1.1597 |      - |   19.4 KB |        1.12 |
| Invalidation_SequentialCommands | 287.50 μs | 19.300 μs | 1.058 μs | 10.81 |    0.09 | 10.2539 |      - |  171.7 KB |        9.94 |
