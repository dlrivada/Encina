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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.129 μs | 0.0497 μs | 0.0744 μs |  1.00 |    0.05 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.810 μs | 0.0522 μs | 0.0765 μs |  1.79 |    0.07 |    2 | 0.1030 | 0.0496 |    1888 B |        2.07 |
| &#39;Validate: SCC agreement&#39;                 | 7.021 μs | 0.1149 μs | 0.1648 μs |  3.30 |    0.14 |    4 | 0.1450 | 0.0687 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.141 μs | 0.2102 μs | 0.3147 μs |  3.36 |    0.18 |    4 | 0.1678 | 0.0839 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 5.915 μs | 0.1608 μs | 0.2356 μs |  2.78 |    0.14 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
