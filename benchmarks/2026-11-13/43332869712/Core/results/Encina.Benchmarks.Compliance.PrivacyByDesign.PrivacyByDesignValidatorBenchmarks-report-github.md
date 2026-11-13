```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   665.9 ns |  62.31 ns | 3.42 ns |  1.00 |    0.01 |    3 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,350.4 ns | 177.67 ns | 9.74 ns |  2.03 |    0.02 |    4 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   534.3 ns | 164.07 ns | 8.99 ns |  0.80 |    0.01 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   280.4 ns |  59.72 ns | 3.27 ns |  0.42 |    0.00 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,174.5 ns | 155.95 ns | 8.55 ns |  1.76 |    0.01 |    4 | 0.1335 |    2264 B |        4.04 |
