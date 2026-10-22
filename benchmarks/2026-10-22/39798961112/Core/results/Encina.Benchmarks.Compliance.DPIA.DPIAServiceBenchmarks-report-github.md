```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                   | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 991.5 ns | 21.24 ns | 30.47 ns |  1.00 |    0.04 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 908.1 ns | 16.04 ns | 22.48 ns |  0.92 |    0.04 |    3 | 0.0257 | 0.0248 |     448 B |        0.97 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 622.6 ns |  8.42 ns | 12.35 ns |  0.63 |    0.02 |    1 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;               | 803.1 ns | 15.87 ns | 23.27 ns |  0.81 |    0.03 |    2 | 0.0257 | 0.0248 |     448 B |        0.97 |
