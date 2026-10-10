```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.77GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,665.3 ns | 1,474.5 ns | 80.82 ns |  1.00 |    0.06 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,486.5 ns |   334.0 ns | 18.31 ns |  0.89 |    0.04 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   973.2 ns |   323.9 ns | 17.75 ns |  0.59 |    0.03 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,396.1 ns |   132.1 ns |  7.24 ns |  0.84 |    0.03 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,360.1 ns |   427.3 ns | 23.42 ns |  1.42 |    0.06 |    2 | 0.0343 | 0.0305 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,422.8 ns |   355.6 ns | 19.49 ns |  0.86 |    0.04 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,366.6 ns |   173.1 ns |  9.49 ns |  0.82 |    0.03 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,223.7 ns |   228.0 ns | 12.50 ns |  0.74 |    0.03 |    1 | 0.0134 | 0.0114 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,224.9 ns |   151.8 ns |  8.32 ns |  0.74 |    0.03 |    1 | 0.0153 | 0.0134 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,117.0 ns |   346.5 ns | 18.99 ns |  0.67 |    0.03 |    1 | 0.0153 | 0.0134 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 1,058.0 ns |   838.5 ns | 45.96 ns |  0.64 |    0.04 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                |   974.0 ns |   331.7 ns | 18.18 ns |  0.59 |    0.03 |    1 | 0.0153 | 0.0143 |     400 B |        0.93 |
