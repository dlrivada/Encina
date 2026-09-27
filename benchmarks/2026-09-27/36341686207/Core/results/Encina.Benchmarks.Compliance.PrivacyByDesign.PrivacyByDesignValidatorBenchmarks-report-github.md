```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   529.9 ns | 110.17 ns |  6.04 ns |  1.00 |    0.01 |    2 | 0.0067 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,241.9 ns | 252.93 ns | 13.86 ns |  2.34 |    0.03 |    3 | 0.0191 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   463.3 ns | 212.79 ns | 11.66 ns |  0.87 |    0.02 |    2 | 0.0105 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   225.3 ns |  41.80 ns |  2.29 ns |  0.43 |    0.01 |    1 | 0.0033 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,100.2 ns | 132.11 ns |  7.24 ns |  2.08 |    0.02 |    3 | 0.0267 |    2264 B |        3.77 |
