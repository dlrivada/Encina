```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 974.4 ns | 923.9 ns | 50.64 ns |  1.00 |    0.06 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 900.8 ns | 554.9 ns | 30.41 ns |  0.93 |    0.05 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 587.3 ns | 449.6 ns | 24.65 ns |  0.60 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 813.1 ns | 438.5 ns | 24.04 ns |  0.84 |    0.04 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
