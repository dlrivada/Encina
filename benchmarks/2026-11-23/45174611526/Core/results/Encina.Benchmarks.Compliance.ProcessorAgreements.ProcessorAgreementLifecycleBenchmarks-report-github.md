```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,911.2 ns |   943.4 ns |  51.71 ns |  1.00 |    0.02 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,436.9 ns |   805.1 ns |  44.13 ns |  0.62 |    0.01 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   927.9 ns | 1,555.2 ns |  85.24 ns |  0.24 |    0.02 |    1 | 0.0038 | 0.0019 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1,056.8 ns |   820.4 ns |  44.97 ns |  0.27 |    0.01 |    1 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   950.3 ns | 2,489.7 ns | 136.47 ns |  0.24 |    0.03 |    1 | 0.0038 | 0.0029 |     376 B |        0.21 |
