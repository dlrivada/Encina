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
| &#39;Validate: compliant request (fast path)&#39;       | 564.0 ns |  40.48 ns |  2.22 ns |  1.00 |    0.00 |    3 | 0.0353 |      - |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 927.6 ns | 224.77 ns | 12.32 ns |  1.64 |    0.02 |    4 | 0.0954 |      - |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 408.5 ns |  38.37 ns |  2.10 ns |  0.72 |    0.00 |    2 | 0.0534 |      - |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 233.2 ns |  15.24 ns |  0.84 ns |  0.41 |    0.00 |    1 | 0.0167 |      - |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 938.2 ns | 190.36 ns | 10.43 ns |  1.66 |    0.02 |    4 | 0.1345 | 0.0010 |    2264 B |        3.77 |
