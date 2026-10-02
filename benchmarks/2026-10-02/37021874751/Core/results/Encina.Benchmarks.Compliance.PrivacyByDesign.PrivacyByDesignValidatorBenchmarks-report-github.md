```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 324.1 ns |  69.25 ns | 3.80 ns |  1.00 |    0.01 |    3 | 0.0358 |      - |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 563.5 ns | 138.39 ns | 7.59 ns |  1.74 |    0.03 |    4 | 0.0954 |      - |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 267.5 ns |  72.65 ns | 3.98 ns |  0.83 |    0.01 |    2 | 0.0534 |      - |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 138.3 ns |  12.85 ns | 0.70 ns |  0.43 |    0.00 |    1 | 0.0167 |      - |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 630.0 ns | 150.61 ns | 8.26 ns |  1.94 |    0.03 |    4 | 0.1345 | 0.0010 |    2264 B |        3.77 |
