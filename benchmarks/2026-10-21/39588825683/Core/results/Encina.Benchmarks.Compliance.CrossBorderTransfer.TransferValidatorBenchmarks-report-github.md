```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.938 μs | 0.8898 μs | 0.0488 μs |  1.00 |    0.02 |    1 | 0.0381 | 0.0343 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 5.015 μs | 1.0615 μs | 0.0582 μs |  1.71 |    0.03 |    2 | 0.0687 | 0.0610 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.543 μs | 3.3661 μs | 0.1845 μs |  2.57 |    0.07 |    3 | 0.0916 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 8.254 μs | 9.6606 μs | 0.5295 μs |  2.81 |    0.16 |    3 | 0.1068 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 7.286 μs | 2.1034 μs | 0.1153 μs |  2.48 |    0.05 |    3 | 0.1144 | 0.1068 |    2944 B |        2.97 |
