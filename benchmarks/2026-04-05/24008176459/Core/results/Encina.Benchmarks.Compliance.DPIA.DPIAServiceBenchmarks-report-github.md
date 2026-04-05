```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                               | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 9.071 ms |    NA |  1.00 |   10 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 8.641 ms |    NA |  0.95 |    3 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             | 8.598 ms |    NA |  0.95 |    2 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 8.742 ms |    NA |  0.96 |    6 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 9.437 ms |    NA |  1.04 |   12 |    2512 B |        5.81 |
| &#39;Reject assessment&#39;                                  | 8.941 ms |    NA |  0.99 |    9 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 9.403 ms |    NA |  1.04 |   11 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 8.856 ms |    NA |  0.98 |    8 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 8.693 ms |    NA |  0.96 |    4 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 8.822 ms |    NA |  0.97 |    7 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 8.727 ms |    NA |  0.96 |    5 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                | 8.579 ms |    NA |  0.95 |    1 |     384 B |        0.89 |
