```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.441 μs | 1.219 μs | 0.0668 μs |  1.00 |    0.03 |    1 | 0.0114 | 0.0076 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.927 μs | 4.745 μs | 0.2601 μs |  1.61 |    0.10 |    2 | 0.0153 | 0.0076 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.665 μs | 1.685 μs | 0.0924 μs |  3.14 |    0.08 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.749 μs | 6.265 μs | 0.3434 μs |  3.18 |    0.14 |    3 | 0.0305 | 0.0153 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.880 μs | 4.289 μs | 0.2351 μs |  2.82 |    0.11 |    3 | 0.0305 | 0.0153 |    2944 B |        2.97 |
