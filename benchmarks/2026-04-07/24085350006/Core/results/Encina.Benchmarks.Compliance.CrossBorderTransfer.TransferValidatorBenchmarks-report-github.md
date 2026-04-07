```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.066 μs | 0.5284 μs | 0.0290 μs |  1.00 |    0.02 |    1 | 0.0496 | 0.0229 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.524 μs | 0.9722 μs | 0.0533 μs |  2.19 |    0.03 |    2 | 0.1030 | 0.0496 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 7.852 μs | 4.6545 μs | 0.2551 μs |  3.80 |    0.12 |    4 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.774 μs | 3.8490 μs | 0.2110 μs |  3.76 |    0.10 |    4 | 0.1678 | 0.0763 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 6.030 μs | 2.0608 μs | 0.1130 μs |  2.92 |    0.06 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
