```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.336 μs | 0.6939 μs | 0.0380 μs |  1.00 |    0.02 |    1 | 0.0572 | 0.0267 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.129 μs | 1.7006 μs | 0.0932 μs |  1.77 |    0.04 |    2 | 0.1068 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.266 μs | 2.7004 μs | 0.1480 μs |  3.11 |    0.07 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.460 μs | 3.4575 μs | 0.1895 μs |  3.19 |    0.08 |    3 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.064 μs | 2.7657 μs | 0.1516 μs |  2.60 |    0.07 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |
