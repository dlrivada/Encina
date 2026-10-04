```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.427 μs | 0.0596 μs | 0.0892 μs |  1.00 |    0.09 |    1 | 0.0591 | 0.0286 |      1 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.478 μs | 0.0865 μs | 0.1294 μs |  1.74 |    0.14 |    2 | 0.1106 | 0.0534 |   1.95 KB |        1.95 |
| &#39;Validate: SCC agreement&#39;                 | 4.578 μs | 0.1071 μs | 0.1603 μs |  3.22 |    0.23 |    5 | 0.1526 | 0.0763 |   2.58 KB |        2.58 |
| &#39;Validate: TIA (deep cascade)&#39;            | 4.076 μs | 0.0421 μs | 0.0590 μs |  2.87 |    0.19 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.89 |
| &#39;Validate: block (full cascade)&#39;          | 3.289 μs | 0.0380 μs | 0.0533 μs |  2.31 |    0.15 |    3 | 0.1755 | 0.0877 |   3.06 KB |        3.06 |
