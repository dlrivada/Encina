```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.742 μs | 0.6039 μs | 0.0331 μs |  1.00 |    0.02 |    1 | 0.0114 | 0.0095 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.931 μs | 2.0769 μs | 0.1138 μs |  1.68 |    0.06 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.121 μs | 1.3256 μs | 0.0727 μs |  2.94 |    0.06 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.689 μs | 1.9391 μs | 0.1063 μs |  3.27 |    0.08 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.036 μs | 1.2812 μs | 0.0702 μs |  2.89 |    0.06 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
