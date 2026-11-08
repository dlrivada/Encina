```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.667 μs | 0.0424 μs | 0.0635 μs |  1.00 |    0.03 |    1 | 0.0381 | 0.0343 |   1.03 KB |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.381 μs | 0.0790 μs | 0.1157 μs |  1.64 |    0.06 |    2 | 0.0687 | 0.0610 |   1.83 KB |        1.77 |
| &#39;Validate: SCC agreement&#39;                 | 6.928 μs | 0.0578 μs | 0.0829 μs |  2.60 |    0.07 |    4 | 0.0992 | 0.0916 |   2.58 KB |        2.50 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.414 μs | 0.0743 μs | 0.1113 μs |  2.78 |    0.08 |    5 | 0.1144 | 0.1068 |   2.89 KB |        2.80 |
| &#39;Validate: block (full cascade)&#39;          | 6.357 μs | 0.0752 μs | 0.1078 μs |  2.38 |    0.07 |    3 | 0.1144 | 0.1068 |   2.88 KB |        2.79 |
