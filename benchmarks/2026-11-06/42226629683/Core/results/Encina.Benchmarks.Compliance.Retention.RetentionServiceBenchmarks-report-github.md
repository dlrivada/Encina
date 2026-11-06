```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.091 μs | 4.4244 μs | 0.2425 μs |  1.00 |    0.09 |    3 | 0.0496 | 0.0229 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.436 μs | 0.1970 μs | 0.0108 μs |  0.47 |    0.03 |    1 | 0.0248 | 0.0114 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.376 μs | 0.1368 μs | 0.0075 μs |  0.77 |    0.05 |    2 | 0.0343 | 0.0305 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.003 μs | 0.2412 μs | 0.0132 μs |  0.65 |    0.04 |    2 | 0.0267 | 0.0229 |     680 B |        0.54 |
