```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   732.9 ns |  9.82 ns | 14.09 ns |  1.00 |    0.03 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,395.1 ns | 14.94 ns | 21.42 ns |  1.90 |    0.05 |    5 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   541.6 ns |  5.10 ns |  7.63 ns |  0.74 |    0.02 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   283.8 ns |  1.34 ns |  1.92 ns |  0.39 |    0.01 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,199.4 ns | 14.47 ns | 21.66 ns |  1.64 |    0.04 |    4 | 0.1335 |    2264 B |        3.77 |
