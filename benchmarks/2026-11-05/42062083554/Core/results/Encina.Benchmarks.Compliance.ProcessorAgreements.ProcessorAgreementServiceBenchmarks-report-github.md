```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1.340 μs | 0.3043 μs | 0.0167 μs |  1.00 |    0.02 |    1 | 0.0134 | 0.0114 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1.344 μs | 0.2471 μs | 0.0135 μs |  1.00 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 1.215 μs | 0.1492 μs | 0.0082 μs |  0.91 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 1.181 μs | 0.2770 μs | 0.0152 μs |  0.88 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 1.046 μs | 0.5962 μs | 0.0327 μs |  0.78 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        1.09 |
