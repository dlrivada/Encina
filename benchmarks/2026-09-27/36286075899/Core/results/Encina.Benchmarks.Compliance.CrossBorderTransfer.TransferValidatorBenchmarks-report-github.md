```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.541 μs | 0.0462 μs | 0.0691 μs |  1.00 |    0.04 |    1 | 0.0572 | 0.0267 |   1.03 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.509 μs | 0.0754 μs | 0.1128 μs |  1.78 |    0.06 |    2 | 0.1068 | 0.0534 |   1.83 KB |        1.77 |
| &#39;Validate: SCC agreement&#39;                 | 7.549 μs | 0.0963 μs | 0.1442 μs |  2.97 |    0.10 |    4 | 0.1526 | 0.0763 |   2.58 KB |        2.50 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.574 μs | 0.1124 μs | 0.1647 μs |  2.98 |    0.10 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.80 |
| &#39;Validate: block (full cascade)&#39;          | 6.239 μs | 0.1186 μs | 0.1775 μs |  2.46 |    0.10 |    3 | 0.1755 | 0.0839 |   2.88 KB |        2.79 |
