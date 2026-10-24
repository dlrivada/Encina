```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,007.4 ns |   383.69 ns |  21.03 ns |  1.00 |    0.01 |    3 | 0.1030 | 0.0496 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 1,983.4 ns | 2,649.83 ns | 145.25 ns |  0.66 |    0.04 |    2 | 0.0648 | 0.0324 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   718.6 ns |   168.57 ns |   9.24 ns |  0.24 |    0.00 |    1 | 0.0219 | 0.0210 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; |   822.1 ns |   245.54 ns |  13.46 ns |  0.27 |    0.00 |    1 | 0.0229 | 0.0219 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   597.5 ns |     8.12 ns |   0.45 ns |  0.20 |    0.00 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |
