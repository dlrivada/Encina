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
| &#39;Validate: adequacy decision (fast path)&#39; | 1.929 μs | 0.4111 μs | 0.0225 μs |  1.00 |    0.01 |    1 | 0.0114 | 0.0095 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.100 μs | 2.4325 μs | 0.1333 μs |  1.61 |    0.06 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.327 μs | 3.3186 μs | 0.1819 μs |  2.76 |    0.09 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.395 μs | 1.2842 μs | 0.0704 μs |  2.80 |    0.04 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 4.967 μs | 6.0078 μs | 0.3293 μs |  2.57 |    0.15 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
