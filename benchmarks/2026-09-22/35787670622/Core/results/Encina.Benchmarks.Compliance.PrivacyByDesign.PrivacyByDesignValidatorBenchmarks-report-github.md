```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   679.8 ns | 212.81 ns | 11.66 ns |  1.00 |    0.02 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,214.5 ns | 161.65 ns |  8.86 ns |  1.79 |    0.03 |    4 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   539.5 ns |  40.35 ns |  2.21 ns |  0.79 |    0.01 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   291.2 ns |  15.69 ns |  0.86 ns |  0.43 |    0.01 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,242.3 ns | 109.17 ns |  5.98 ns |  1.83 |    0.03 |    4 | 0.1335 |    2264 B |        3.77 |
