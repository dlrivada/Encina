```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,909.2 ns |   368.8 ns |  20.22 ns |  1.00 |    0.01 |    4 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,419.1 ns |   127.0 ns |   6.96 ns |  0.62 |    0.00 |    3 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1,003.6 ns |   880.6 ns |  48.27 ns |  0.26 |    0.01 |    2 | 0.0038 | 0.0029 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; | 1,121.8 ns |   903.5 ns |  49.53 ns |  0.29 |    0.01 |    2 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   864.8 ns | 2,221.3 ns | 121.76 ns |  0.22 |    0.03 |    1 | 0.0038 | 0.0029 |     376 B |        0.21 |
