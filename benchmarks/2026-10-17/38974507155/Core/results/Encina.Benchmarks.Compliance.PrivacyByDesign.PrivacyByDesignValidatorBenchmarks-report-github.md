```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 383.8 ns |   9.91 ns | 0.54 ns |  1.00 |    0.00 |    2 | 0.0067 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 882.8 ns |  70.22 ns | 3.85 ns |  2.30 |    0.01 |    3 | 0.0181 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               | 321.8 ns |   7.75 ns | 0.42 ns |  0.84 |    0.00 |    2 | 0.0105 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          | 184.4 ns | 172.84 ns | 9.47 ns |  0.48 |    0.02 |    1 | 0.0033 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 794.6 ns |  12.48 ns | 0.68 ns |  2.07 |    0.00 |    3 | 0.0267 |    2264 B |        4.04 |
