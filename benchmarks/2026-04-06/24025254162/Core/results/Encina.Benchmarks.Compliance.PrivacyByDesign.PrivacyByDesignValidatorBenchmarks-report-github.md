```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   671.8 ns |  90.92 ns |  4.98 ns |  1.00 |    0.01 |    2 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,321.7 ns | 652.64 ns | 35.77 ns |  1.97 |    0.05 |    3 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   569.5 ns | 320.67 ns | 17.58 ns |  0.85 |    0.02 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   288.0 ns |  25.34 ns |  1.39 ns |  0.43 |    0.00 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,186.5 ns | 142.34 ns |  7.80 ns |  1.77 |    0.02 |    3 | 0.1335 |    2264 B |        3.77 |
