```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   718.9 ns |  30.59 ns | 1.68 ns |  1.00 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,363.2 ns |  94.50 ns | 5.18 ns |  1.90 |    4 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   527.6 ns |   6.25 ns | 0.34 ns |  0.73 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   294.9 ns |  16.02 ns | 0.88 ns |  0.41 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,209.0 ns | 105.53 ns | 5.78 ns |  1.68 |    4 | 0.1335 |    2264 B |        3.77 |
