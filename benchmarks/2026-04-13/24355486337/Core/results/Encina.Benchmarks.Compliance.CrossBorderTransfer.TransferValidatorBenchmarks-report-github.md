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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.276 μs | 0.0528 μs | 0.0791 μs |  1.00 |    0.05 |    1 | 0.0496 | 0.0229 |     944 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.067 μs | 0.1663 μs | 0.2488 μs |  1.79 |    0.12 |    2 | 0.0992 | 0.0458 |    1760 B |        1.86 |
| &#39;Validate: SCC agreement&#39;                 | 7.134 μs | 0.2185 μs | 0.3134 μs |  3.14 |    0.17 |    4 | 0.1450 | 0.0687 |    2528 B |        2.68 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.149 μs | 0.2152 μs | 0.3086 μs |  3.14 |    0.17 |    4 | 0.1678 | 0.0839 |    2848 B |        3.02 |
| &#39;Validate: block (full cascade)&#39;          | 5.698 μs | 0.0740 μs | 0.1013 μs |  2.51 |    0.10 |    3 | 0.1678 | 0.0839 |    2832 B |        3.00 |
