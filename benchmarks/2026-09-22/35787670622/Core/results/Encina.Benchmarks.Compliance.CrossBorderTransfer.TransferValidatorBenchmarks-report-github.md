```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.558 μs | 0.2861 μs | 0.0157 μs |  1.00 |    0.01 |    1 | 0.0515 | 0.0248 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.951 μs | 0.4529 μs | 0.0248 μs |  1.89 |    0.02 |    2 | 0.1030 | 0.0496 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 5.583 μs | 0.6253 μs | 0.0343 μs |  3.58 |    0.04 |    4 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.574 μs | 1.0486 μs | 0.0575 μs |  3.58 |    0.04 |    4 | 0.1678 | 0.0839 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 4.461 μs | 0.9657 μs | 0.0529 μs |  2.86 |    0.04 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
