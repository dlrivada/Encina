```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.311 μs | 0.0601 μs | 0.0900 μs |  1.00 |    0.05 |    1 | 0.0496 | 0.0229 |     944 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.105 μs | 0.2012 μs | 0.3012 μs |  1.78 |    0.15 |    2 | 0.0992 | 0.0458 |    1760 B |        1.86 |
| &#39;Validate: SCC agreement&#39;                 | 7.487 μs | 0.1821 μs | 0.2669 μs |  3.25 |    0.17 |    4 | 0.1450 | 0.0687 |    2528 B |        2.68 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.267 μs | 0.1771 μs | 0.2651 μs |  3.15 |    0.17 |    4 | 0.1678 | 0.0839 |    2848 B |        3.02 |
| &#39;Validate: block (full cascade)&#39;          | 6.450 μs | 0.1582 μs | 0.2368 μs |  2.80 |    0.15 |    3 | 0.1678 | 0.0839 |    2832 B |        3.00 |
