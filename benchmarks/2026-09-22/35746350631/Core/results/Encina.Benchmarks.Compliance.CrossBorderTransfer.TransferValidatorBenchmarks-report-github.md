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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.081 μs | 0.3686 μs | 0.0202 μs |  1.00 |    0.01 |    1 | 0.0515 | 0.0248 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.119 μs | 1.9521 μs | 0.1070 μs |  1.98 |    0.05 |    2 | 0.0992 | 0.0458 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 7.592 μs | 1.0306 μs | 0.0565 μs |  3.65 |    0.04 |    3 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.823 μs | 1.4270 μs | 0.0782 μs |  3.76 |    0.05 |    3 | 0.1678 | 0.0763 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 6.400 μs | 2.9174 μs | 0.1599 μs |  3.08 |    0.07 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
