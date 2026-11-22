```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.044 μs | 0.0651 μs | 0.0974 μs |  1.00 |    0.07 |    1 | 0.0572 | 0.0267 |   1.03 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.659 μs | 0.0725 μs | 0.1086 μs |  1.79 |    0.10 |    2 | 0.1106 | 0.0534 |   1.95 KB |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.879 μs | 0.1104 μs | 0.1653 μs |  3.37 |    0.18 |    4 | 0.1526 | 0.0763 |   2.58 KB |        2.50 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.671 μs | 0.1865 μs | 0.2791 μs |  3.27 |    0.20 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.80 |
| &#39;Validate: block (full cascade)&#39;          | 5.369 μs | 0.1671 μs | 0.2449 μs |  2.63 |    0.17 |    3 | 0.1755 | 0.0839 |   2.88 KB |        2.79 |
