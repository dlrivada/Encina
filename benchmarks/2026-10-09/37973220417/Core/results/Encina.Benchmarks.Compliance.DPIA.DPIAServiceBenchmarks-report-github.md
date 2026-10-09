```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      |   929.2 ns |   821.4 ns | 45.02 ns |  1.00 |    0.06 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,099.9 ns |   673.6 ns | 36.92 ns |  1.19 |    0.06 |    2 | 0.0048 | 0.0038 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   702.5 ns |   203.4 ns | 11.15 ns |  0.76 |    0.03 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,077.8 ns |   963.7 ns | 52.82 ns |  1.16 |    0.07 |    2 | 0.0048 | 0.0038 |     448 B |        1.04 |
| &#39;Approve assessment&#39;                                 | 1,673.9 ns |   584.5 ns | 32.04 ns |  1.80 |    0.08 |    3 | 0.0095 | 0.0076 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  |   961.3 ns | 1,702.4 ns | 93.31 ns |  1.04 |    0.10 |    2 | 0.0038 | 0.0029 |     384 B |        0.89 |
| &#39;Request revision&#39;                                   |   979.1 ns |   267.5 ns | 14.66 ns |  1.06 |    0.05 |    2 | 0.0038 | 0.0029 |     384 B |        0.89 |
| &#39;Expire assessment&#39;                                  |   945.3 ns |   724.1 ns | 39.69 ns |  1.02 |    0.06 |    2 | 0.0038 | 0.0029 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   902.4 ns |   803.1 ns | 44.02 ns |  0.97 |    0.06 |    2 | 0.0048 | 0.0038 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   721.9 ns |   186.9 ns | 10.24 ns |  0.78 |    0.03 |    1 | 0.0038 | 0.0029 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   685.5 ns |   652.9 ns | 35.79 ns |  0.74 |    0.05 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   671.9 ns |   389.0 ns | 21.32 ns |  0.72 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
