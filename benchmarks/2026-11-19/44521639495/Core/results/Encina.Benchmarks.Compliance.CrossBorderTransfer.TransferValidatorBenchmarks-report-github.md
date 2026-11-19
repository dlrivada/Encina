```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.498 μs | 3.906 μs | 0.2141 μs |  1.00 |    0.10 |    1 | 0.0114 | 0.0076 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.065 μs | 3.977 μs | 0.2180 μs |  1.64 |    0.14 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.016 μs | 2.632 μs | 0.1443 μs |  2.82 |    0.21 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.373 μs | 2.292 μs | 0.1256 μs |  2.97 |    0.22 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.633 μs | 4.698 μs | 0.2575 μs |  2.67 |    0.21 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
