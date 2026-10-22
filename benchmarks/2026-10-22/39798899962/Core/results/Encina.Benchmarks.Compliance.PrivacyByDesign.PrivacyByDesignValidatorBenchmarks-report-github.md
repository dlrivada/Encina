```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   669.1 ns | 32.67 ns | 1.79 ns |  1.00 |    2 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,364.0 ns | 47.09 ns | 2.58 ns |  2.04 |    3 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   568.7 ns | 46.25 ns | 2.53 ns |  0.85 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   286.7 ns | 21.63 ns | 1.19 ns |  0.43 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,238.2 ns | 73.00 ns | 4.00 ns |  1.85 |    3 | 0.1335 |    2264 B |        4.04 |
