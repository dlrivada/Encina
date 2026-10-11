```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   692.8 ns |  6.77 ns |  9.50 ns |   699.9 ns |  1.00 |    0.02 |    3 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,403.1 ns |  3.18 ns |  4.66 ns | 1,403.4 ns |  2.03 |    0.03 |    5 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   553.4 ns |  1.13 ns |  1.66 ns |   553.0 ns |  0.80 |    0.01 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   283.0 ns |  0.77 ns |  1.13 ns |   283.0 ns |  0.41 |    0.01 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,268.3 ns | 11.64 ns | 17.06 ns | 1,258.4 ns |  1.83 |    0.03 |    4 | 0.1335 |    2264 B |        4.04 |
