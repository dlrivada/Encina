```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.578 μs | 0.2908 μs | 0.0159 μs |  1.00 |    0.01 |    1 | 0.0381 | 0.0343 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.257 μs | 4.1402 μs | 0.2269 μs |  1.65 |    0.08 |    2 | 0.0687 | 0.0610 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.654 μs | 1.7348 μs | 0.0951 μs |  2.58 |    0.03 |    3 | 0.0992 | 0.0916 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.310 μs | 0.4293 μs | 0.0235 μs |  2.84 |    0.02 |    3 | 0.1144 | 0.1068 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.519 μs | 3.1778 μs | 0.1742 μs |  2.53 |    0.06 |    3 | 0.1144 | 0.1068 |    2944 B |        2.97 |
