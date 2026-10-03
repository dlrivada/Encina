```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.716 μs | 0.7895 μs | 0.0433 μs |  1.00 |    0.03 |    1 | 0.0114 | 0.0095 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.875 μs | 1.5361 μs | 0.0842 μs |  1.68 |    0.06 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.170 μs | 1.0146 μs | 0.0556 μs |  3.01 |    0.07 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 5.435 μs | 2.5343 μs | 0.1389 μs |  3.17 |    0.10 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.063 μs | 3.5876 μs | 0.1967 μs |  2.95 |    0.12 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
