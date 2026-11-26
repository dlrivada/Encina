```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 372.6 ns |  36.15 ns |  1.98 ns |  1.00 |    0.01 |    2 | 0.0334 |      - |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 719.9 ns | 759.00 ns | 41.60 ns |  1.93 |    0.10 |    3 | 0.0935 |      - |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 317.7 ns |  35.93 ns |  1.97 ns |  0.85 |    0.01 |    2 | 0.0534 |      - |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 159.6 ns | 169.32 ns |  9.28 ns |  0.43 |    0.02 |    1 | 0.0167 |      - |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 774.4 ns | 251.79 ns | 13.80 ns |  2.08 |    0.03 |    3 | 0.1345 | 0.0010 |    2264 B |        4.04 |
