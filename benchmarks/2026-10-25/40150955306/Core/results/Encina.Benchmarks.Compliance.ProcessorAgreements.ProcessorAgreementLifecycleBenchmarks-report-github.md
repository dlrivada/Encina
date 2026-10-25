```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4,192.5 ns | 59.65 ns | 89.28 ns |  1.00 |    0.03 |    5 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,610.8 ns | 47.77 ns | 68.50 ns |  0.62 |    0.02 |    4 | 0.0648 | 0.0305 |    1168 B |        0.66 |
| &#39;AuditDPA (record audit)&#39;      | 1,049.1 ns | 18.18 ns | 26.65 ns |  0.25 |    0.01 |    2 | 0.0210 | 0.0191 |     400 B |        0.23 |
| &#39;RenewDPA (extend expiration)&#39; | 1,124.1 ns | 25.44 ns | 37.30 ns |  0.27 |    0.01 |    3 | 0.0229 | 0.0210 |     424 B |        0.24 |
| &#39;TerminateDPA (end agreement)&#39; |   974.9 ns | 17.59 ns | 25.22 ns |  0.23 |    0.01 |    1 | 0.0210 | 0.0191 |     392 B |        0.22 |
