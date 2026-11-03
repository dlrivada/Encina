```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,665.4 ns | 464.0 ns | 25.43 ns |  1.00 |    0.02 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,505.8 ns | 105.8 ns |  5.80 ns |  0.90 |    0.01 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   979.0 ns | 144.7 ns |  7.93 ns |  0.59 |    0.01 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,413.2 ns | 137.1 ns |  7.52 ns |  0.85 |    0.01 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
