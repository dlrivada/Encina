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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.044 μs | 0.0219 μs | 0.0321 μs |  1.00 |    0.02 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.809 μs | 0.0800 μs | 0.1197 μs |  1.86 |    0.06 |    2 | 0.1030 | 0.0496 |    1888 B |        2.07 |
| &#39;Validate: SCC agreement&#39;                 | 6.852 μs | 0.1073 μs | 0.1572 μs |  3.35 |    0.09 |    4 | 0.1450 | 0.0687 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.931 μs | 0.1111 μs | 0.1663 μs |  3.39 |    0.10 |    4 | 0.1678 | 0.0839 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 5.466 μs | 0.0671 μs | 0.0941 μs |  2.68 |    0.06 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
