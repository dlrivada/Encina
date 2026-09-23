```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.134 μs | 0.3555 μs | 0.0195 μs |  1.00 |    0.01 |    1 | 0.0496 | 0.0229 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.023 μs | 4.4194 μs | 0.2422 μs |  1.89 |    0.10 |    2 | 0.0992 | 0.0458 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 7.060 μs | 0.6114 μs | 0.0335 μs |  3.31 |    0.03 |    4 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.215 μs | 1.2081 μs | 0.0662 μs |  3.38 |    0.04 |    4 | 0.1678 | 0.0839 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 5.839 μs | 1.8847 μs | 0.1033 μs |  2.74 |    0.05 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
