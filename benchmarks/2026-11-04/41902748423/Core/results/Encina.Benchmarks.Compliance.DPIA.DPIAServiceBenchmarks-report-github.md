```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1.655 μs | 0.5453 μs | 0.0299 μs |  1.00 |    0.02 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1.511 μs | 0.8774 μs | 0.0481 μs |  0.91 |    0.03 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 1.036 μs | 0.7699 μs | 0.0422 μs |  0.63 |    0.02 |    1 | 0.0191 | 0.0172 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1.465 μs | 1.3579 μs | 0.0744 μs |  0.89 |    0.04 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
