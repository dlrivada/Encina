```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,405.1 ns | 859.6 ns | 47.12 ns |  1.00 |    0.02 |    3 | 0.1030 | 0.0496 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,199.0 ns | 819.4 ns | 44.91 ns |  0.65 |    0.01 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   773.1 ns | 180.4 ns |  9.89 ns |  0.23 |    0.00 |    1 | 0.0219 | 0.0210 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; |   820.1 ns | 578.8 ns | 31.73 ns |  0.24 |    0.01 |    1 | 0.0229 | 0.0219 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   691.8 ns | 198.4 ns | 10.87 ns |  0.20 |    0.00 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |
