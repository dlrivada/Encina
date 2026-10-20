```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   432.6 ns | 329.05 ns | 18.04 ns |  1.00 |    0.05 |    2 | 0.0067 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,079.3 ns | 727.14 ns | 39.86 ns |  2.50 |    0.12 |    3 | 0.0172 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   444.9 ns | 339.14 ns | 18.59 ns |  1.03 |    0.05 |    2 | 0.0105 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   180.9 ns |   7.00 ns |  0.38 ns |  0.42 |    0.01 |    1 | 0.0033 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    |   938.6 ns | 106.20 ns |  5.82 ns |  2.17 |    0.08 |    3 | 0.0267 |    2264 B |        4.04 |
