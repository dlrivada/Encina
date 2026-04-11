```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error   | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|--------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   677.5 ns | 2.62 ns |  3.92 ns |  1.00 |    0.01 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,310.3 ns | 5.27 ns |  7.56 ns |  1.93 |    0.02 |    5 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   555.4 ns | 4.02 ns |  6.02 ns |  0.82 |    0.01 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   281.2 ns | 1.93 ns |  2.82 ns |  0.42 |    0.00 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,198.7 ns | 7.94 ns | 11.88 ns |  1.77 |    0.02 |    4 | 0.1335 |    2264 B |        3.77 |
