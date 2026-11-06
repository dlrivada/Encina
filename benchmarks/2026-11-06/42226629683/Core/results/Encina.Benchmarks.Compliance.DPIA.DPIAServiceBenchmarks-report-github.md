```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,260.2 ns | 2,182.11 ns | 119.61 ns |  1.01 |    0.11 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,100.7 ns |   433.33 ns |  23.75 ns |  0.88 |    0.07 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   748.2 ns |    94.86 ns |   5.20 ns |  0.60 |    0.05 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 1,082.4 ns | 1,050.15 ns |  57.56 ns |  0.86 |    0.08 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
