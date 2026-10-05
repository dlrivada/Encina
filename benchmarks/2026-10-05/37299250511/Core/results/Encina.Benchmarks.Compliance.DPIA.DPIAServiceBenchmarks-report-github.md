```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,430.4 ns |   486.5 ns |  26.67 ns |  1.00 |    0.02 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,263.1 ns |   517.8 ns |  28.38 ns |  0.88 |    0.02 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   966.6 ns |   327.3 ns |  17.94 ns |  0.68 |    0.02 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,181.7 ns |   711.5 ns |  39.00 ns |  0.83 |    0.03 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,179.1 ns | 1,774.5 ns |  97.27 ns |  1.52 |    0.06 |    2 | 0.0076 | 0.0038 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,274.5 ns | 3,303.9 ns | 181.10 ns |  0.89 |    0.11 |    1 | 0.0038 | 0.0019 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,210.2 ns | 1,770.1 ns |  97.02 ns |  0.85 |    0.06 |    1 | 0.0038 | 0.0019 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,110.8 ns | 3,016.6 ns | 165.35 ns |  0.78 |    0.10 |    1 | 0.0038 | 0.0019 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      |   976.5 ns | 1,325.2 ns |  72.64 ns |  0.68 |    0.05 |    1 | 0.0038 | 0.0019 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   981.0 ns |   194.7 ns |  10.67 ns |  0.69 |    0.01 |    1 | 0.0038 | 0.0019 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   974.6 ns |   764.3 ns |  41.89 ns |  0.68 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   946.4 ns |   698.5 ns |  38.29 ns |  0.66 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
