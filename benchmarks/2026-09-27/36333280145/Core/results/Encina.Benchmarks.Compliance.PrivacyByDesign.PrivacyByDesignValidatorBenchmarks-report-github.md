```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   460.2 ns |  63.90 ns | 3.50 ns |  1.00 |    0.01 |    2 | 0.0072 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,055.4 ns | 155.27 ns | 8.51 ns |  2.29 |    0.02 |    3 | 0.0191 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   428.8 ns |  73.21 ns | 4.01 ns |  0.93 |    0.01 |    2 | 0.0105 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   185.4 ns |  24.09 ns | 1.32 ns |  0.40 |    0.00 |    1 | 0.0033 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,030.7 ns | 101.74 ns | 5.58 ns |  2.24 |    0.02 |    3 | 0.0267 |    2264 B |        3.77 |
