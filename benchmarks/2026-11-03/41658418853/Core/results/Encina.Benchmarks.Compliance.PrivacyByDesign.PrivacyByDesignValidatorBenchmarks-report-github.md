```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 536.2 ns |  30.03 ns |  1.65 ns |  1.00 |    0.00 |    3 | 0.0334 |      - |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 919.5 ns |  68.46 ns |  3.75 ns |  1.71 |    0.01 |    4 | 0.0935 |      - |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 424.6 ns |  14.05 ns |  0.77 ns |  0.79 |    0.00 |    2 | 0.0534 |      - |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 222.8 ns |  30.61 ns |  1.68 ns |  0.42 |    0.00 |    1 | 0.0167 |      - |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 959.4 ns | 307.25 ns | 16.84 ns |  1.79 |    0.03 |    4 | 0.1345 | 0.0010 |    2264 B |        4.04 |
