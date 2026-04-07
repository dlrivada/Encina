```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                          | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       | 3.413 ms |    NA |  1.00 |    3 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 3.422 ms |    NA |  1.00 |    4 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               | 3.345 ms |    NA |  0.98 |    1 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          | 3.411 ms |    NA |  1.00 |    2 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 3.538 ms |    NA |  1.04 |    5 |    2264 B |        3.77 |
