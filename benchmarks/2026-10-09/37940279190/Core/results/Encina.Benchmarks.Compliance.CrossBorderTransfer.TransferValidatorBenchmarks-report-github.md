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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.345 μs | 0.3246 μs | 0.0178 μs |  1.00 |    0.01 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.048 μs | 0.1790 μs | 0.0098 μs |  1.73 |    0.01 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.954 μs | 1.8466 μs | 0.1012 μs |  2.97 |    0.04 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.417 μs | 2.3257 μs | 0.1275 μs |  3.16 |    0.05 |    3 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.115 μs | 1.3315 μs | 0.0730 μs |  2.61 |    0.03 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
