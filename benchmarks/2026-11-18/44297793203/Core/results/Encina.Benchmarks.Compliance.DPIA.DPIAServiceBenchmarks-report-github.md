```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,096.2 ns |   263.7 ns | 14.45 ns |  1.00 |    0.02 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      |   899.6 ns | 1,403.1 ns | 76.91 ns |  0.82 |    0.06 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   810.7 ns |   232.7 ns | 12.76 ns |  0.74 |    0.01 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               |   909.4 ns |   554.8 ns | 30.41 ns |  0.83 |    0.03 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
