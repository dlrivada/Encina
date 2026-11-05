```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   684.4 ns |  16.92 ns | 0.93 ns |  1.00 |    3 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,361.2 ns |  51.37 ns | 2.82 ns |  1.99 |    4 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   545.4 ns |  39.38 ns | 2.16 ns |  0.80 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   274.3 ns |  17.50 ns | 0.96 ns |  0.40 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,240.5 ns | 172.02 ns | 9.43 ns |  1.81 |    4 | 0.1335 |    2264 B |        4.04 |
