```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.400 μs | 0.0384 μs | 0.0551 μs |  1.00 |    0.03 |    1 | 0.0572 | 0.0267 |   1.03 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.143 μs | 0.0643 μs | 0.0962 μs |  1.73 |    0.06 |    2 | 0.1068 | 0.0534 |   1.83 KB |        1.77 |
| &#39;Validate: SCC agreement&#39;                 | 6.932 μs | 0.1087 μs | 0.1627 μs |  2.89 |    0.09 |    4 | 0.1526 | 0.0763 |   2.58 KB |        2.50 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.029 μs | 0.0696 μs | 0.0975 μs |  2.93 |    0.08 |    4 | 0.1755 | 0.0839 |   2.89 KB |        2.80 |
| &#39;Validate: block (full cascade)&#39;          | 5.919 μs | 0.1788 μs | 0.2676 μs |  2.47 |    0.12 |    3 | 0.1755 | 0.0839 |   2.88 KB |        2.79 |
