```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,417.9 ns |   560.8 ns |  30.74 ns |  1.00 |    0.03 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,258.8 ns |   535.4 ns |  29.35 ns |  0.89 |    0.02 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   916.2 ns | 2,015.6 ns | 110.48 ns |  0.65 |    0.07 |    1 | 0.0038 | 0.0019 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,198.3 ns |   978.2 ns |  53.62 ns |  0.85 |    0.04 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
