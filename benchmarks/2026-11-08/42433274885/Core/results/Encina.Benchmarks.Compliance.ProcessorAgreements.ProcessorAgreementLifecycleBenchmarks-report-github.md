```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 2,991.2 ns | 17.87 ns | 25.62 ns |  1.00 |    5 | 0.1030 | 0.0496 |    1840 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 1,826.2 ns | 16.55 ns | 23.20 ns |  0.61 |    4 | 0.0648 | 0.0324 |    1136 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   682.8 ns |  3.84 ns |  5.51 ns |  0.23 |    2 | 0.0219 | 0.0210 |     384 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; |   730.8 ns |  6.44 ns |  8.81 ns |  0.24 |    3 | 0.0229 | 0.0219 |     408 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; |   613.1 ns |  4.40 ns |  6.02 ns |  0.20 |    1 | 0.0210 | 0.0200 |     376 B |        0.20 |
