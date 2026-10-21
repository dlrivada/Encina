```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   492.6 ns |  66.72 ns |  3.66 ns |  1.00 |    0.01 |    2 | 0.0067 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,133.4 ns |  90.34 ns |  4.95 ns |  2.30 |    0.02 |    3 | 0.0172 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   433.5 ns | 161.44 ns |  8.85 ns |  0.88 |    0.02 |    2 | 0.0105 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   213.5 ns |  32.29 ns |  1.77 ns |  0.43 |    0.00 |    1 | 0.0033 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,024.1 ns | 806.24 ns | 44.19 ns |  2.08 |    0.08 |    3 | 0.0267 |    2264 B |        4.04 |
