```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   688.1 ns |  31.07 ns | 1.70 ns |  1.00 |    3 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,413.9 ns |  95.23 ns | 5.22 ns |  2.05 |    4 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   554.9 ns |  28.75 ns | 1.58 ns |  0.81 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   274.7 ns |   1.13 ns | 0.06 ns |  0.40 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,241.1 ns | 175.59 ns | 9.62 ns |  1.80 |    4 | 0.1335 |    2264 B |        4.04 |
