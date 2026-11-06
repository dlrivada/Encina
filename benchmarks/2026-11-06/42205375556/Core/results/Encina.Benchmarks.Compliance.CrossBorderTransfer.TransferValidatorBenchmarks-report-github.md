```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.351 μs | 2.518 μs | 0.1380 μs |  1.00 |    0.07 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.230 μs | 1.664 μs | 0.0912 μs |  1.80 |    0.10 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.514 μs | 1.560 μs | 0.0855 μs |  3.20 |    0.16 |    4 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.751 μs | 7.923 μs | 0.4343 μs |  3.30 |    0.23 |    4 | 0.1678 | 0.0763 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.137 μs | 2.106 μs | 0.1154 μs |  2.62 |    0.14 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
