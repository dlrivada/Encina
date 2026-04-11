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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.126 μs | 0.0415 μs | 0.0608 μs |  1.00 |    0.04 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.757 μs | 0.0591 μs | 0.0885 μs |  1.77 |    0.06 |    2 | 0.1030 | 0.0496 |    1888 B |        2.07 |
| &#39;Validate: SCC agreement&#39;                 | 6.744 μs | 0.1009 μs | 0.1479 μs |  3.18 |    0.11 |    4 | 0.1450 | 0.0687 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.735 μs | 0.0415 μs | 0.0608 μs |  3.17 |    0.09 |    4 | 0.1678 | 0.0839 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 5.409 μs | 0.0327 μs | 0.0459 μs |  2.55 |    0.07 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
