```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   457.7 ns | 34.75 ns | 1.90 ns |  1.00 |    2 | 0.0067 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,042.3 ns | 97.93 ns | 5.37 ns |  2.28 |    3 | 0.0172 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   441.9 ns | 33.73 ns | 1.85 ns |  0.97 |    2 | 0.0105 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   189.6 ns | 10.10 ns | 0.55 ns |  0.41 |    1 | 0.0033 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    |   961.2 ns | 19.43 ns | 1.06 ns |  2.10 |    3 | 0.0267 |    2264 B |        4.04 |
