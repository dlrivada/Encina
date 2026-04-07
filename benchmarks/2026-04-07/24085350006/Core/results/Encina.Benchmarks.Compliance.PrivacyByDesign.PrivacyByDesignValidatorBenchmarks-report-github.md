```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   667.9 ns | 57.09 ns | 3.13 ns |  1.00 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,277.4 ns | 66.04 ns | 3.62 ns |  1.91 |    4 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   542.2 ns | 39.34 ns | 2.16 ns |  0.81 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   276.0 ns |  9.14 ns | 0.50 ns |  0.41 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,157.1 ns | 83.54 ns | 4.58 ns |  1.73 |    4 | 0.1335 |    2264 B |        3.77 |
