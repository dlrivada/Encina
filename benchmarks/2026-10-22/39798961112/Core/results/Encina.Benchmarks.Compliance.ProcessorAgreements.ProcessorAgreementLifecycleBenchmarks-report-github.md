```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,984.6 ns | 16.09 ns | 24.09 ns |  1.00 |    5 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,461.6 ns | 14.98 ns | 21.96 ns |  0.62 |    4 | 0.0648 | 0.0305 |    1168 B |        0.66 |
| &#39;AuditDPA (record audit)&#39;      |   988.7 ns |  7.15 ns | 10.48 ns |  0.25 |    2 | 0.0210 | 0.0191 |     400 B |        0.23 |
| &#39;RenewDPA (extend expiration)&#39; | 1,058.9 ns | 12.73 ns | 18.65 ns |  0.27 |    3 | 0.0229 | 0.0210 |     424 B |        0.24 |
| &#39;TerminateDPA (end agreement)&#39; |   900.0 ns |  8.64 ns | 12.94 ns |  0.23 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |
