```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   677.7 ns |  2.81 ns |  4.03 ns |   678.2 ns |  1.00 |    0.01 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,161.7 ns |  4.26 ns |  5.97 ns | 1,161.2 ns |  1.71 |    0.01 |    4 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   524.5 ns |  8.67 ns | 12.43 ns |   524.9 ns |  0.77 |    0.02 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   294.3 ns |  5.77 ns |  8.09 ns |   300.2 ns |  0.43 |    0.01 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,182.3 ns | 12.23 ns | 17.92 ns | 1,175.8 ns |  1.74 |    0.03 |    4 | 0.1335 |    2264 B |        3.77 |
