```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.540 μs | 0.0547 μs | 0.0802 μs |  1.00 |    0.04 |    1 | 0.0572 | 0.0267 |   1.03 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.227 μs | 0.1111 μs | 0.1663 μs |  1.67 |    0.08 |    2 | 0.1106 | 0.0534 |   1.95 KB |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.253 μs | 0.1540 μs | 0.2306 μs |  2.86 |    0.12 |    4 | 0.1526 | 0.0763 |   2.58 KB |        2.50 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.403 μs | 0.1498 μs | 0.2243 μs |  2.92 |    0.12 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.80 |
| &#39;Validate: block (full cascade)&#39;          | 5.959 μs | 0.1068 μs | 0.1497 μs |  2.35 |    0.09 |    3 | 0.1755 | 0.0839 |   2.88 KB |        2.79 |
