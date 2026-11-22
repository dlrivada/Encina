```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                   | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1.444 μs | 0.0394 μs | 0.0590 μs |  1.00 |    0.06 |    2 | 0.0038 | 0.0019 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1.211 μs | 0.0396 μs | 0.0593 μs |  0.84 |    0.05 |    1 | 0.0038 | 0.0019 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 1.109 μs | 0.0440 μs | 0.0659 μs |  0.77 |    0.05 |    1 | 0.0038 | 0.0029 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;               | 1.168 μs | 0.0234 μs | 0.0350 μs |  0.81 |    0.04 |    1 | 0.0038 | 0.0019 |     464 B |        1.00 |
