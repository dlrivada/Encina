```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                               | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 9.078 ms |    NA |  1.00 |   11 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 8.801 ms |    NA |  0.97 |   10 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             | 8.269 ms |    NA |  0.91 |    1 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 8.739 ms |    NA |  0.96 |    8 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 9.114 ms |    NA |  1.00 |   12 |    2512 B |        5.81 |
| &#39;Reject assessment&#39;                                  | 8.647 ms |    NA |  0.95 |    6 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 8.764 ms |    NA |  0.97 |    9 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 8.700 ms |    NA |  0.96 |    7 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 8.410 ms |    NA |  0.93 |    3 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 8.413 ms |    NA |  0.93 |    4 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 8.370 ms |    NA |  0.92 |    2 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                | 8.438 ms |    NA |  0.93 |    5 |     384 B |        0.89 |
