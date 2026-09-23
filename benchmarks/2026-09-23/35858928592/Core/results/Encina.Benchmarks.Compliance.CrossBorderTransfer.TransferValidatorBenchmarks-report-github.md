```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.081 μs | 1.2456 μs | 0.0683 μs |  1.00 |    0.04 |    1 | 0.0496 | 0.0229 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.874 μs | 4.9654 μs | 0.2722 μs |  1.86 |    0.12 |    2 | 0.0992 | 0.0458 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 7.030 μs | 3.8602 μs | 0.2116 μs |  3.38 |    0.13 |    3 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.350 μs | 0.5358 μs | 0.0294 μs |  3.53 |    0.10 |    3 | 0.1678 | 0.0763 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 6.514 μs | 0.8837 μs | 0.0484 μs |  3.13 |    0.09 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
