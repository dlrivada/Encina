```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,455.2 ns | 1,439.5 ns | 78.90 ns |  1.00 |    0.03 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,253.0 ns |   506.2 ns | 27.75 ns |  0.65 |    0.01 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1,024.0 ns |   691.8 ns | 37.92 ns |  0.30 |    0.01 |    1 | 0.0038 | 0.0029 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; | 1,038.5 ns | 1,173.5 ns | 64.32 ns |  0.30 |    0.02 |    1 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   959.6 ns |   482.2 ns | 26.43 ns |  0.28 |    0.01 |    1 | 0.0038 | 0.0029 |     376 B |        0.21 |
