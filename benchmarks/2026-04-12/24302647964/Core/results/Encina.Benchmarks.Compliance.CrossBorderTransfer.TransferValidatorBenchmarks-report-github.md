```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.256 μs | 0.0662 μs | 0.0990 μs |  1.00 |    0.06 |    1 | 0.0515 | 0.0248 |     912 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.285 μs | 0.2510 μs | 0.3679 μs |  1.90 |    0.18 |    2 | 0.0992 | 0.0458 |    1760 B |        1.93 |
| &#39;Validate: SCC agreement&#39;                 | 7.676 μs | 0.1398 μs | 0.2093 μs |  3.41 |    0.17 |    4 | 0.1450 | 0.0687 |    2528 B |        2.77 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.302 μs | 0.0855 μs | 0.1254 μs |  3.24 |    0.15 |    4 | 0.1678 | 0.0839 |    2848 B |        3.12 |
| &#39;Validate: block (full cascade)&#39;          | 5.588 μs | 0.0569 μs | 0.0797 μs |  2.48 |    0.11 |    3 | 0.1678 | 0.0839 |    2832 B |        3.11 |
