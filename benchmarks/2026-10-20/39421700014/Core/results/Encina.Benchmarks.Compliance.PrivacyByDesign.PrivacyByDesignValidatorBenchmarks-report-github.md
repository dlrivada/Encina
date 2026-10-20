```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   670.6 ns | 100.96 ns |  5.53 ns |  1.00 |    0.01 |    2 | 0.0334 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,366.2 ns |  48.60 ns |  2.66 ns |  2.04 |    0.01 |    3 | 0.0935 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   567.8 ns |  36.90 ns |  2.02 ns |  0.85 |    0.01 |    2 | 0.0534 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   279.7 ns |  26.24 ns |  1.44 ns |  0.42 |    0.00 |    1 | 0.0167 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,287.3 ns | 883.91 ns | 48.45 ns |  1.92 |    0.06 |    3 | 0.1335 |    2264 B |        4.04 |
