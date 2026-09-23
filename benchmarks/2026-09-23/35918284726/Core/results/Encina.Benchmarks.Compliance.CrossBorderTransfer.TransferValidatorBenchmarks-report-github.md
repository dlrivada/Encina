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
| &#39;Validate: adequacy decision (fast path)&#39; | 1.948 μs | 0.0528 μs | 0.0029 μs |  1.00 |    0.00 |    1 | 0.0515 | 0.0248 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.543 μs | 0.3337 μs | 0.0183 μs |  1.82 |    0.01 |    2 | 0.1030 | 0.0496 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 6.269 μs | 0.8964 μs | 0.0491 μs |  3.22 |    0.02 |    3 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.677 μs | 0.7722 μs | 0.0423 μs |  3.43 |    0.02 |    3 | 0.1678 | 0.0839 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 5.470 μs | 0.0748 μs | 0.0041 μs |  2.81 |    0.00 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
