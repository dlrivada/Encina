```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,564.0 ns |  45.60 ns |  2.50 ns |  1.00 |    0.00 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,442.1 ns | 506.26 ns | 27.75 ns |  0.92 |    0.02 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   985.5 ns | 217.97 ns | 11.95 ns |  0.63 |    0.01 |    1 | 0.0191 | 0.0172 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,310.3 ns | 329.50 ns | 18.06 ns |  0.84 |    0.01 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
