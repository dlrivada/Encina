```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   434.4 ns |  52.88 ns |  2.90 ns |  1.00 |    0.01 |    2 | 0.0072 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,005.3 ns | 195.49 ns | 10.72 ns |  2.31 |    0.03 |    3 | 0.0191 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   414.3 ns | 130.18 ns |  7.14 ns |  0.95 |    0.02 |    2 | 0.0105 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   177.1 ns |  39.06 ns |  2.14 ns |  0.41 |    0.00 |    1 | 0.0033 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    |   839.8 ns | 116.41 ns |  6.38 ns |  1.93 |    0.02 |    3 | 0.0267 |    2264 B |        3.77 |
