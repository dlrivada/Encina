```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,792.6 ns | 1,110.5 ns | 60.87 ns |  1.00 |    0.02 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,401.2 ns | 1,372.4 ns | 75.23 ns |  0.63 |    0.02 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   944.6 ns |   110.8 ns |  6.07 ns |  0.25 |    0.00 |    1 | 0.0038 | 0.0019 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1,057.9 ns | 1,100.2 ns | 60.30 ns |  0.28 |    0.01 |    1 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   925.6 ns | 1,318.1 ns | 72.25 ns |  0.24 |    0.02 |    1 | 0.0038 | 0.0029 |     376 B |        0.21 |
