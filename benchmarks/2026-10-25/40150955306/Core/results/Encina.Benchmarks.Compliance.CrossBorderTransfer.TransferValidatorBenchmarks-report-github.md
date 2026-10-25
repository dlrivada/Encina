```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.484 μs | 0.0233 μs | 0.0348 μs |  1.00 |    0.03 |    1 | 0.0591 | 0.0286 |      1 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.854 μs | 0.1080 μs | 0.1550 μs |  1.92 |    0.11 |    2 | 0.1106 | 0.0534 |   1.95 KB |        1.95 |
| &#39;Validate: SCC agreement&#39;                 | 5.066 μs | 0.1710 μs | 0.2506 μs |  3.42 |    0.18 |    5 | 0.1526 | 0.0763 |   2.58 KB |        2.58 |
| &#39;Validate: TIA (deep cascade)&#39;            | 4.444 μs | 0.0793 μs | 0.1188 μs |  3.00 |    0.10 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.89 |
| &#39;Validate: block (full cascade)&#39;          | 3.536 μs | 0.0672 μs | 0.1006 μs |  2.38 |    0.09 |    3 | 0.1755 | 0.0877 |   3.06 KB |        3.06 |
