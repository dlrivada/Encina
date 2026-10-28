```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.788 μs | 0.2072 μs | 0.0114 μs |  1.00 |    0.00 |    1 | 0.0381 | 0.0343 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.406 μs | 1.2757 μs | 0.0699 μs |  1.58 |    0.02 |    2 | 0.0687 | 0.0610 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.080 μs | 2.3009 μs | 0.1261 μs |  2.54 |    0.04 |    3 | 0.0992 | 0.0916 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.519 μs | 0.9890 μs | 0.0542 μs |  2.70 |    0.02 |    3 | 0.1144 | 0.1068 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.795 μs | 3.0062 μs | 0.1648 μs |  2.44 |    0.05 |    3 | 0.1144 | 0.1068 |    2944 B |        2.97 |
