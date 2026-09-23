```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   461.5 ns |  97.00 ns |  5.32 ns |  1.00 |    0.01 |    2 | 0.0072 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,048.7 ns |  78.83 ns |  4.32 ns |  2.27 |    0.02 |    3 | 0.0191 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   430.0 ns |  35.10 ns |  1.92 ns |  0.93 |    0.01 |    2 | 0.0105 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   205.6 ns |   7.04 ns |  0.39 ns |  0.45 |    0.00 |    1 | 0.0033 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,004.6 ns | 489.15 ns | 26.81 ns |  2.18 |    0.05 |    3 | 0.0267 |    2264 B |        3.77 |
