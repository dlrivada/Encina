```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   603.4 ns |  1.85 ns |  2.53 ns |  1.00 |    0.01 |    2 | 0.0238 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,281.0 ns |  8.19 ns | 12.26 ns |  2.12 |    0.02 |    3 | 0.0629 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   582.0 ns |  6.52 ns |  9.55 ns |  0.96 |    0.02 |    2 | 0.0353 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   270.5 ns |  1.89 ns |  2.83 ns |  0.45 |    0.00 |    1 | 0.0110 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,386.1 ns | 12.23 ns | 17.93 ns |  2.30 |    0.03 |    4 | 0.0896 |    2264 B |        3.77 |
