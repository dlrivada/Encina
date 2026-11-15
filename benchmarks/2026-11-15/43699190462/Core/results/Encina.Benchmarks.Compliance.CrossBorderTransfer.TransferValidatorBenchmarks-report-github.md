```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 2.073 μs | 0.7088 μs | 0.0389 μs |  1.00 |    0.02 |    1 | 0.0114 | 0.0076 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.455 μs | 2.6746 μs | 0.1466 μs |  1.67 |    0.07 |    2 | 0.0191 | 0.0153 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 5.685 μs | 1.3973 μs | 0.0766 μs |  2.74 |    0.06 |    3 | 0.0305 | 0.0229 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 6.140 μs | 1.5270 μs | 0.0837 μs |  2.96 |    0.06 |    3 | 0.0305 | 0.0229 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.542 μs | 2.0351 μs | 0.1115 μs |  2.67 |    0.06 |    3 | 0.0305 | 0.0229 |    2944 B |        2.97 |
