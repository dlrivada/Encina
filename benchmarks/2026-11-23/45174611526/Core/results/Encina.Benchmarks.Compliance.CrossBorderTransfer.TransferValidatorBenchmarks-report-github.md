```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.725 μs | 0.7385 μs | 0.0405 μs |  1.00 |    0.03 |    1 | 0.0114 | 0.0095 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.731 μs | 2.3573 μs | 0.1292 μs |  1.58 |    0.07 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 4.772 μs | 0.5470 μs | 0.0300 μs |  2.77 |    0.06 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.109 μs | 2.6861 μs | 0.1472 μs |  2.96 |    0.09 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 4.696 μs | 0.7488 μs | 0.0410 μs |  2.72 |    0.06 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
