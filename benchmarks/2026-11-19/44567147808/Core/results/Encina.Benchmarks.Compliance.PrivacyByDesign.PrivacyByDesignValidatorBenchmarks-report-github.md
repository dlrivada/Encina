```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   451.3 ns |  7.35 ns | 0.40 ns |  1.00 |    2 | 0.0067 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,051.8 ns | 33.34 ns | 1.83 ns |  2.33 |    3 | 0.0172 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   437.6 ns | 37.25 ns | 2.04 ns |  0.97 |    2 | 0.0105 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   201.8 ns | 12.66 ns | 0.69 ns |  0.45 |    1 | 0.0033 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    |   955.6 ns | 47.75 ns | 2.62 ns |  2.12 |    3 | 0.0267 |    2264 B |        4.04 |
