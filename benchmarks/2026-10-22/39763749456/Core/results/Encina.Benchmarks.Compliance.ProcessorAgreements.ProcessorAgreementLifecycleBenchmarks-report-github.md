```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,199.1 ns | 144.37 ns |  7.91 ns |  1.00 |    3 | 0.1030 | 0.0496 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 1,820.7 ns | 266.99 ns | 14.63 ns |  0.57 |    2 | 0.0648 | 0.0324 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   744.9 ns | 100.44 ns |  5.51 ns |  0.23 |    1 | 0.0219 | 0.0210 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; |   790.5 ns | 277.90 ns | 15.23 ns |  0.25 |    1 | 0.0229 | 0.0219 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   720.1 ns |  65.86 ns |  3.61 ns |  0.23 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |
