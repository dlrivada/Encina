```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   658.3 ns |  46.68 ns | 2.56 ns |  1.00 |    3 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,313.5 ns |  39.70 ns | 2.18 ns |  2.00 |    4 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   547.4 ns |  18.99 ns | 1.04 ns |  0.83 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   265.4 ns |   1.07 ns | 0.06 ns |  0.40 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,187.5 ns | 162.79 ns | 8.92 ns |  1.80 |    4 | 0.1335 |    2264 B |        4.04 |
