```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,486.4 ns |  67.12 ns |  3.68 ns |  1.00 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,340.7 ns | 243.73 ns | 13.36 ns |  0.90 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   944.6 ns | 193.70 ns | 10.62 ns |  0.64 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 1,278.2 ns |  25.79 ns |  1.41 ns |  0.86 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
