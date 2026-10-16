```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   450.3 ns | 620.8 ns | 34.03 ns |  1.00 |    0.10 |    2 | 0.0067 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,049.4 ns | 799.1 ns | 43.80 ns |  2.34 |    0.18 |    3 | 0.0172 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   428.5 ns | 256.2 ns | 14.04 ns |  0.96 |    0.07 |    2 | 0.0105 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   184.7 ns | 244.6 ns | 13.41 ns |  0.41 |    0.04 |    1 | 0.0033 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    |   932.3 ns | 718.5 ns | 39.39 ns |  2.08 |    0.16 |    3 | 0.0267 |    2264 B |        4.04 |
