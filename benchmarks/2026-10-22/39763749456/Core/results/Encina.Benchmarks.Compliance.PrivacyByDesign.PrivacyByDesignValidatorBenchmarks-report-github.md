```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   604.9 ns | 56.13 ns | 3.08 ns |  1.00 |    2 | 0.0219 |     560 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,294.3 ns | 47.71 ns | 2.62 ns |  2.14 |    3 | 0.0610 |    1568 B |        2.80 |
| &#39;Analyze: data minimization only&#39;               |   559.2 ns |  9.51 ns | 0.52 ns |  0.92 |    2 | 0.0353 |     896 B |        1.60 |
| &#39;Validate: default privacy inspection&#39;          |   271.6 ns |  9.56 ns | 0.52 ns |  0.45 |    1 | 0.0110 |     280 B |        0.50 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,450.0 ns | 29.28 ns | 1.60 ns |  2.40 |    3 | 0.0896 |    2264 B |        4.04 |
