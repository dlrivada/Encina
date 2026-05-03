```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.225 μs | 0.0637 μs | 0.0953 μs |  1.00 |    0.06 |    1 | 0.0496 | 0.0229 |     944 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.770 μs | 0.0638 μs | 0.0956 μs |  1.70 |    0.08 |    2 | 0.1030 | 0.0496 |    1888 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 6.762 μs | 0.1016 μs | 0.1521 μs |  3.04 |    0.15 |    4 | 0.1450 | 0.0687 |    2528 B |        2.68 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.877 μs | 0.0990 μs | 0.1451 μs |  3.10 |    0.15 |    4 | 0.1678 | 0.0839 |    2848 B |        3.02 |
| &#39;Validate: block (full cascade)&#39;          | 5.477 μs | 0.0319 μs | 0.0477 μs |  2.47 |    0.11 |    3 | 0.1678 | 0.0839 |    2832 B |        3.00 |
