```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 545.5 ns | 388.59 ns | 21.30 ns |  1.00 |    0.05 |    3 | 0.0353 |      - |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 931.1 ns | 131.17 ns |  7.19 ns |  1.71 |    0.06 |    4 | 0.0954 |      - |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 427.1 ns |  83.14 ns |  4.56 ns |  0.78 |    0.03 |    2 | 0.0534 |      - |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 217.7 ns |  12.99 ns |  0.71 ns |  0.40 |    0.01 |    1 | 0.0167 |      - |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 929.3 ns | 411.66 ns | 22.56 ns |  1.71 |    0.07 |    4 | 0.1345 | 0.0010 |    2264 B |        3.77 |
