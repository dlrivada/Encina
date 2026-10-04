```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.451 μs | 1.175 μs | 0.0644 μs |  1.00 |    0.05 |    1 | 0.0591 | 0.0286 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.712 μs | 1.140 μs | 0.0625 μs |  1.87 |    0.08 |    2 | 0.1106 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.197 μs | 2.003 μs | 0.1098 μs |  3.59 |    0.15 |    4 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.215 μs | 1.286 μs | 0.0705 μs |  3.60 |    0.15 |    4 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 4.174 μs | 2.268 μs | 0.1243 μs |  2.88 |    0.13 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
