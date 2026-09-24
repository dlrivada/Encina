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
| &#39;Validate: adequacy decision (fast path)&#39; | 2.685 μs | 0.5961 μs | 0.0327 μs |  1.00 |    0.01 |    1 | 0.0381 | 0.0343 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 4.493 μs | 1.5970 μs | 0.0875 μs |  1.67 |    0.03 |    2 | 0.0687 | 0.0610 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.921 μs | 2.3156 μs | 0.1269 μs |  2.58 |    0.05 |    3 | 0.0992 | 0.0916 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.673 μs | 0.4279 μs | 0.0235 μs |  2.86 |    0.03 |    3 | 0.1144 | 0.1068 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 6.757 μs | 2.3285 μs | 0.1276 μs |  2.52 |    0.05 |    3 | 0.1144 | 0.1068 |    2944 B |        2.97 |
