```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error    | StdDev  | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|---------:|--------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 547.8 ns | 19.19 ns | 1.05 ns |  1.00 |    3 | 0.0334 |      - |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 899.0 ns | 47.95 ns | 2.63 ns |  1.64 |    4 | 0.0935 |      - |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 394.5 ns | 29.83 ns | 1.64 ns |  0.72 |    2 | 0.0534 |      - |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 219.6 ns | 11.91 ns | 0.65 ns |  0.40 |    1 | 0.0167 |      - |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 929.4 ns | 91.61 ns | 5.02 ns |  1.70 |    4 | 0.1345 | 0.0010 |    2264 B |        4.04 |
