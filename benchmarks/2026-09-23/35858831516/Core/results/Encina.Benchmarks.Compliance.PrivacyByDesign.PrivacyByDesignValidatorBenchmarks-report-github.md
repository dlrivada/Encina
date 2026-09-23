```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 518.8 ns | 191.86 ns | 10.52 ns |  1.00 |    0.02 |    3 | 0.0353 |      - |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 904.8 ns |  98.10 ns |  5.38 ns |  1.74 |    0.03 |    4 | 0.0954 |      - |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 399.1 ns |  40.66 ns |  2.23 ns |  0.77 |    0.01 |    2 | 0.0534 |      - |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 218.6 ns |  61.23 ns |  3.36 ns |  0.42 |    0.01 |    1 | 0.0167 |      - |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 927.4 ns |  73.76 ns |  4.04 ns |  1.79 |    0.03 |    4 | 0.1345 | 0.0010 |    2264 B |        3.77 |
