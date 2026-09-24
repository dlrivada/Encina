```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 513.1 ns |  10.92 ns | 0.60 ns |  1.00 |    0.00 |    3 | 0.0353 |      - |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 894.5 ns |  78.75 ns | 4.32 ns |  1.74 |    0.01 |    4 | 0.0954 |      - |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 392.1 ns |  35.17 ns | 1.93 ns |  0.76 |    0.00 |    2 | 0.0534 |      - |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 218.0 ns |  10.01 ns | 0.55 ns |  0.42 |    0.00 |    1 | 0.0167 |      - |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 912.3 ns | 170.30 ns | 9.33 ns |  1.78 |    0.02 |    4 | 0.1345 | 0.0010 |    2264 B |        3.77 |
