```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1.309 μs | 0.0428 μs | 0.0023 μs |  1.00 |    1 | 0.0134 | 0.0114 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1.329 μs | 0.0924 μs | 0.0051 μs |  1.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 1.241 μs | 0.1857 μs | 0.0102 μs |  0.95 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 1.195 μs | 0.1789 μs | 0.0098 μs |  0.91 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 1.040 μs | 0.3732 μs | 0.0205 μs |  0.79 |    1 | 0.0153 | 0.0134 |     384 B |        1.09 |
