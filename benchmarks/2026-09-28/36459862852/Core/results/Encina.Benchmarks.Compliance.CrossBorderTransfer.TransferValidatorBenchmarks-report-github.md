```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.248 μs | 0.6644 μs | 0.0364 μs |  1.00 |    0.02 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.145 μs | 1.6058 μs | 0.0880 μs |  1.84 |    0.04 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.719 μs | 2.8363 μs | 0.1555 μs |  2.99 |    0.07 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.285 μs | 0.9404 μs | 0.0515 μs |  3.24 |    0.05 |    3 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.067 μs | 1.6586 μs | 0.0909 μs |  2.70 |    0.05 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
