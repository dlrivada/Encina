```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error   | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|--------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   674.5 ns | 4.45 ns |  6.53 ns |  1.00 |    0.01 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,340.4 ns | 8.41 ns | 12.59 ns |  1.99 |    0.03 |    5 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   556.6 ns | 3.40 ns |  4.98 ns |  0.83 |    0.01 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   285.6 ns | 2.08 ns |  3.05 ns |  0.42 |    0.01 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,205.0 ns | 8.95 ns | 13.40 ns |  1.79 |    0.03 |    4 | 0.1335 |    2264 B |        3.77 |
