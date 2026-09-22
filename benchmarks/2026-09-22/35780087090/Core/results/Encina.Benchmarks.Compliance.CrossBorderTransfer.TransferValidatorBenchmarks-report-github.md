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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.419 μs | 3.4142 μs | 0.1871 μs |  1.00 |    0.09 |    1 | 0.0496 | 0.0229 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.967 μs | 7.2037 μs | 0.3949 μs |  2.06 |    0.19 |    2 | 0.0992 | 0.0458 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 7.850 μs | 8.1723 μs | 0.4480 μs |  3.26 |    0.26 |    3 | 0.1373 | 0.0610 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 8.293 μs | 3.9290 μs | 0.2154 μs |  3.44 |    0.23 |    3 | 0.1678 | 0.0763 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 6.936 μs | 0.9739 μs | 0.0534 μs |  2.88 |    0.19 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
