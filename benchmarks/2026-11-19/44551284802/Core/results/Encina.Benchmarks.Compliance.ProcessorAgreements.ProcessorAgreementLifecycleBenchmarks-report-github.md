```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,542.8 ns | 1,065.0 ns |  58.38 ns |  1.00 |    0.02 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,247.1 ns |   933.7 ns |  51.18 ns |  0.63 |    0.02 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   872.3 ns | 1,834.1 ns | 100.53 ns |  0.25 |    0.02 |    1 | 0.0038 | 0.0029 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; |   923.4 ns |   767.0 ns |  42.04 ns |  0.26 |    0.01 |    1 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   945.7 ns |   964.3 ns |  52.86 ns |  0.27 |    0.01 |    1 | 0.0038 | 0.0029 |     376 B |        0.21 |
