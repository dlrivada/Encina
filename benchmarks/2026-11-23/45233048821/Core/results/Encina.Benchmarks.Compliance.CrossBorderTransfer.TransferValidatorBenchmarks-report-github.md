```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.407 μs | 0.3750 μs | 0.0206 μs |  1.00 |    0.01 |    1 | 0.0114 | 0.0076 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.050 μs | 3.1260 μs | 0.1713 μs |  1.68 |    0.06 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.533 μs | 1.4823 μs | 0.0812 μs |  2.71 |    0.04 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.177 μs | 2.8155 μs | 0.1543 μs |  2.98 |    0.06 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.924 μs | 1.7256 μs | 0.0946 μs |  2.88 |    0.04 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
