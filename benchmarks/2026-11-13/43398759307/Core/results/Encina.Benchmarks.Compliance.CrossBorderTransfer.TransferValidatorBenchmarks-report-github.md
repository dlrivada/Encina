```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.718 μs |  1.1525 μs | 0.0632 μs |  1.00 |    0.04 |    1 | 0.0591 | 0.0286 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.883 μs |  2.5669 μs | 0.1407 μs |  2.26 |    0.10 |    2 | 0.1106 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.854 μs |  6.0025 μs | 0.3290 μs |  3.41 |    0.20 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.022 μs |  0.8186 μs | 0.0449 μs |  3.51 |    0.11 |    3 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.383 μs | 11.3223 μs | 0.6206 μs |  3.14 |    0.33 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
