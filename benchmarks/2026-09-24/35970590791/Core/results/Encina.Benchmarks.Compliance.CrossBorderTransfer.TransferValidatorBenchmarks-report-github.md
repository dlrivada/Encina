```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.356 μs | 1.029 μs | 0.0564 μs |  1.00 |    0.05 |    1 | 0.0591 | 0.0286 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.562 μs | 2.398 μs | 0.1315 μs |  1.89 |    0.11 |    2 | 0.1106 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 4.938 μs | 6.619 μs | 0.3628 μs |  3.65 |    0.27 |    4 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 4.649 μs | 3.914 μs | 0.2145 μs |  3.43 |    0.18 |    4 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 3.640 μs | 2.300 μs | 0.1261 μs |  2.69 |    0.12 |    3 | 0.1755 | 0.0916 |    2944 B |        2.97 |
