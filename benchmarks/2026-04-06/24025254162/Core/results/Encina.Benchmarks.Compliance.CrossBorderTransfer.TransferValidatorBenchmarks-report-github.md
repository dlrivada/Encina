```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.975 μs | 0.3177 μs | 0.0174 μs |  1.00 |    0.01 |    1 | 0.0515 | 0.0248 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.602 μs | 0.1679 μs | 0.0092 μs |  1.82 |    0.01 |    2 | 0.1030 | 0.0496 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 6.404 μs | 0.5285 μs | 0.0290 μs |  3.24 |    0.03 |    3 | 0.1450 | 0.0687 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.804 μs | 1.4728 μs | 0.0807 μs |  3.45 |    0.04 |    3 | 0.1678 | 0.0839 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 5.561 μs | 0.6335 μs | 0.0347 μs |  2.82 |    0.03 |    3 | 0.1678 | 0.0839 |    2832 B |        3.22 |
