```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.294 μs |  0.5500 μs | 0.0301 μs |  1.00 |    0.02 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.049 μs |  1.3318 μs | 0.0730 μs |  1.77 |    0.03 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.079 μs |  2.0934 μs | 0.1147 μs |  3.09 |    0.06 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.464 μs | 10.3478 μs | 0.5672 μs |  3.25 |    0.22 |    3 | 0.1678 | 0.0763 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.948 μs |  2.5122 μs | 0.1377 μs |  2.59 |    0.06 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
