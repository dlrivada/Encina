```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.428 μs | 0.8900 μs | 0.0488 μs |  1.00 |    0.02 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.594 μs | 1.9067 μs | 0.1045 μs |  1.89 |    0.05 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 8.235 μs | 3.8541 μs | 0.2113 μs |  3.39 |    0.10 |    4 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 8.306 μs | 9.2797 μs | 0.5087 μs |  3.42 |    0.19 |    4 | 0.1678 | 0.0763 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.425 μs | 3.9770 μs | 0.2180 μs |  2.65 |    0.09 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
