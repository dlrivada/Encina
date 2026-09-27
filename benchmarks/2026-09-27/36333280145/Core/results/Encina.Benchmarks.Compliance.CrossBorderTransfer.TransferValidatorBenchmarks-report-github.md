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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.842 μs | 0.3512 μs | 0.0192 μs |  1.00 |    0.01 |    1 | 0.0381 | 0.0343 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.702 μs | 1.2600 μs | 0.0691 μs |  1.65 |    0.02 |    2 | 0.0687 | 0.0610 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 7.328 μs | 3.3406 μs | 0.1831 μs |  2.58 |    0.06 |    3 | 0.0992 | 0.0916 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.873 μs | 2.5385 μs | 0.1391 μs |  2.77 |    0.05 |    3 | 0.1068 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 7.113 μs | 2.5755 μs | 0.1412 μs |  2.50 |    0.05 |    3 | 0.1144 | 0.1068 |    2944 B |        2.97 |
