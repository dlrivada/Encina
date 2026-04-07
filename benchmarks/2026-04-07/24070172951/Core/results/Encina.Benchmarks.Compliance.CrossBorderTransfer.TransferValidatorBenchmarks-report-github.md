```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.063 μs | 0.5545 μs | 0.0304 μs |  1.00 |    0.02 |    1 | 0.0496 | 0.0229 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.813 μs | 0.7578 μs | 0.0415 μs |  1.85 |    0.03 |    2 | 0.1030 | 0.0496 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 6.676 μs | 1.7532 μs | 0.0961 μs |  3.24 |    0.06 |    3 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.146 μs | 1.6732 μs | 0.0917 μs |  3.46 |    0.06 |    3 | 0.1678 | 0.0839 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 5.955 μs | 2.9047 μs | 0.1592 μs |  2.89 |    0.08 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
