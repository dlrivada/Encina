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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.415 μs | 0.4963 μs | 0.0272 μs |  1.00 |    0.01 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.366 μs | 1.1202 μs | 0.0614 μs |  1.81 |    0.03 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.569 μs | 1.1450 μs | 0.0628 μs |  3.13 |    0.04 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 8.186 μs | 3.7602 μs | 0.2061 μs |  3.39 |    0.08 |    3 | 0.1678 | 0.0763 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.920 μs | 1.4443 μs | 0.0792 μs |  2.87 |    0.04 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
