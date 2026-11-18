```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 356.7 ns |  89.10 ns |  4.88 ns |  1.00 |    0.02 |    2 | 0.0334 |      - |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 623.0 ns | 433.24 ns | 23.75 ns |  1.75 |    0.06 |    3 | 0.0935 |      - |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 301.6 ns |  76.80 ns |  4.21 ns |  0.85 |    0.01 |    2 | 0.0534 |      - |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 153.6 ns | 124.59 ns |  6.83 ns |  0.43 |    0.02 |    1 | 0.0167 |      - |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 683.3 ns |  63.87 ns |  3.50 ns |  1.92 |    0.02 |    3 | 0.1345 | 0.0010 |    2264 B |        4.04 |
