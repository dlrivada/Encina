```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 5.116 μs | 0.0276 μs | 0.0405 μs |  1.00 |    4 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.131 μs | 0.0225 μs | 0.0329 μs |  0.61 |    3 | 0.0648 | 0.0305 |    1168 B |        0.66 |
| &#39;AuditDPA (record audit)&#39;      | 1.247 μs | 0.0138 μs | 0.0202 μs |  0.24 |    2 | 0.0210 | 0.0191 |     400 B |        0.23 |
| &#39;RenewDPA (extend expiration)&#39; | 1.293 μs | 0.0150 μs | 0.0225 μs |  0.25 |    2 | 0.0229 | 0.0210 |     424 B |        0.24 |
| &#39;TerminateDPA (end agreement)&#39; | 1.115 μs | 0.0145 μs | 0.0217 μs |  0.22 |    1 | 0.0210 | 0.0191 |     392 B |        0.22 |
