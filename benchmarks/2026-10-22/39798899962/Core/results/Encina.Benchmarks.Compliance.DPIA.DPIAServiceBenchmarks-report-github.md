```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,639.2 ns |   162.3 ns |   8.89 ns |  1.00 |    0.01 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,560.0 ns | 2,347.4 ns | 128.67 ns |  0.95 |    0.07 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   982.1 ns |   335.3 ns |  18.38 ns |  0.60 |    0.01 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,415.3 ns |   162.5 ns |   8.91 ns |  0.86 |    0.01 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
