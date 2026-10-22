```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.496 μs | 0.0601 μs | 0.0900 μs |  1.00 |    0.05 |    1 | 0.0572 | 0.0267 |   1.03 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.367 μs | 0.0910 μs | 0.1363 μs |  1.75 |    0.08 |    2 | 0.1068 | 0.0534 |   1.83 KB |        1.77 |
| &#39;Validate: SCC agreement&#39;                 | 7.602 μs | 0.1937 μs | 0.2899 μs |  3.05 |    0.16 |    4 | 0.1526 | 0.0763 |   2.58 KB |        2.50 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.440 μs | 0.1445 μs | 0.2118 μs |  2.98 |    0.13 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.80 |
| &#39;Validate: block (full cascade)&#39;          | 6.320 μs | 0.0874 μs | 0.1253 μs |  2.53 |    0.10 |    3 | 0.1755 | 0.0839 |   2.88 KB |        2.79 |
