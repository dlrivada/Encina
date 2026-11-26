```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.506 μs | 0.2490 μs | 0.0137 μs |  1.00 |    0.01 |    1 | 0.0114 | 0.0076 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.258 μs | 2.8026 μs | 0.1536 μs |  1.70 |    0.05 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.226 μs | 4.2196 μs | 0.2313 μs |  2.88 |    0.08 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.753 μs | 2.0040 μs | 0.1098 μs |  3.09 |    0.04 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 7.117 μs | 2.2502 μs | 0.1233 μs |  2.84 |    0.04 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
