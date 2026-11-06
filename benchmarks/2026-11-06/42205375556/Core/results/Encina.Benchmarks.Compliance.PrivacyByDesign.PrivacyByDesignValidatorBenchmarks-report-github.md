```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 520.9 ns |  13.01 ns | 0.71 ns |  1.00 |    3 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 963.7 ns |  17.67 ns | 0.97 ns |  1.85 |    4 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 432.7 ns |  39.02 ns | 2.14 ns |  0.83 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 223.5 ns |  10.22 ns | 0.56 ns |  0.43 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 994.3 ns | 126.94 ns | 6.96 ns |  1.91 |    4 | 0.1335 |    2264 B |        4.04 |
