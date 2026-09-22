```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.381 μs | 0.8282 μs | 0.0454 μs |  1.00 |    0.02 |    1 | 0.0343 | 0.0305 |     880 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.296 μs | 0.9993 μs | 0.0548 μs |  1.80 |    0.04 |    2 | 0.0687 | 0.0610 |    1760 B |        2.00 |
| &#39;Validate: SCC agreement&#39;                 | 7.050 μs | 2.3657 μs | 0.1297 μs |  2.96 |    0.07 |    3 | 0.0992 | 0.0916 |    2528 B |        2.87 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.252 μs | 0.7506 μs | 0.0411 μs |  3.05 |    0.05 |    3 | 0.1068 | 0.0992 |    2848 B |        3.24 |
| &#39;Validate: block (full cascade)&#39;          | 6.532 μs | 2.2762 μs | 0.1248 μs |  2.74 |    0.06 |    3 | 0.1068 | 0.0992 |    2832 B |        3.22 |
