```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev  | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|--------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 340.3 ns |   7.38 ns | 0.40 ns |  1.00 |    3 | 0.0358 |      - |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 572.6 ns |  32.71 ns | 1.79 ns |  1.68 |    4 | 0.0954 |      - |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 273.1 ns | 102.31 ns | 5.61 ns |  0.80 |    2 | 0.0534 |      - |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 141.9 ns |  10.50 ns | 0.58 ns |  0.42 |    1 | 0.0167 |      - |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 653.1 ns |  73.24 ns | 4.01 ns |  1.92 |    4 | 0.1345 | 0.0010 |    2264 B |        3.77 |
