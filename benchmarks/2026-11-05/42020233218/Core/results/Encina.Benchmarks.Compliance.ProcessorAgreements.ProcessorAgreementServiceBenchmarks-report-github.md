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
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1.285 μs | 0.1747 μs | 0.0096 μs |  1.00 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1.347 μs | 0.3757 μs | 0.0206 μs |  1.05 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 1.210 μs | 0.3439 μs | 0.0188 μs |  0.94 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 1.175 μs | 0.2567 μs | 0.0141 μs |  0.91 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 1.033 μs | 0.5517 μs | 0.0302 μs |  0.80 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        1.09 |
