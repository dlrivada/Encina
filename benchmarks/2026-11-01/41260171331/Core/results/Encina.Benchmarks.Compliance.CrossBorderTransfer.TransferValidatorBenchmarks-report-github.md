```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.764 μs | 0.0720 μs | 0.1055 μs |  1.00 |    0.08 |    1 | 0.0591 | 0.0286 |      1 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.380 μs | 0.1446 μs | 0.2027 μs |  1.92 |    0.16 |    2 | 0.1106 | 0.0534 |   1.95 KB |        1.95 |
| &#39;Validate: SCC agreement&#39;                 | 5.797 μs | 0.2284 μs | 0.3349 μs |  3.30 |    0.26 |    4 | 0.1526 | 0.0763 |   2.58 KB |        2.58 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.100 μs | 0.2052 μs | 0.3008 μs |  3.47 |    0.26 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.89 |
| &#39;Validate: block (full cascade)&#39;          | 4.210 μs | 0.2150 μs | 0.3218 μs |  2.39 |    0.22 |    3 | 0.1755 | 0.0839 |   2.88 KB |        2.88 |
