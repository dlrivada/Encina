```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.291 μs | 0.1230 μs | 0.0067 μs |  1.00 |    0.01 |    1 | 0.0591 | 0.0286 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 2.365 μs | 0.6955 μs | 0.0381 μs |  1.83 |    0.03 |    2 | 0.1106 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 4.239 μs | 1.2655 μs | 0.0694 μs |  3.28 |    0.05 |    4 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 4.183 μs | 1.7081 μs | 0.0936 μs |  3.24 |    0.06 |    4 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 3.345 μs | 0.2013 μs | 0.0110 μs |  2.59 |    0.01 |    3 | 0.1755 | 0.0916 |    2944 B |        2.97 |
