```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 525.6 ns |  36.28 ns |  1.99 ns |  1.00 |    0.00 |    3 | 0.0334 |      - |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 951.5 ns | 123.96 ns |  6.79 ns |  1.81 |    0.01 |    4 | 0.0935 |      - |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 425.5 ns | 183.47 ns | 10.06 ns |  0.81 |    0.02 |    2 | 0.0534 |      - |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 221.3 ns |   2.96 ns |  0.16 ns |  0.42 |    0.00 |    1 | 0.0167 |      - |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 947.9 ns | 243.23 ns | 13.33 ns |  1.80 |    0.02 |    4 | 0.1345 | 0.0010 |    2264 B |        4.04 |
