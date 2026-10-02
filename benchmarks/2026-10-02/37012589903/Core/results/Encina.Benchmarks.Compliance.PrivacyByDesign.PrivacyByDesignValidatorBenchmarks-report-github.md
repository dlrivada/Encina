```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   698.8 ns |  40.77 ns |  2.23 ns |  1.00 |    0.00 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,337.8 ns |  53.70 ns |  2.94 ns |  1.91 |    0.01 |    4 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   547.1 ns |  45.53 ns |  2.50 ns |  0.78 |    0.00 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   294.4 ns |  12.80 ns |  0.70 ns |  0.42 |    0.00 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,238.0 ns | 313.34 ns | 17.18 ns |  1.77 |    0.02 |    4 | 0.1335 |    2264 B |        3.77 |
