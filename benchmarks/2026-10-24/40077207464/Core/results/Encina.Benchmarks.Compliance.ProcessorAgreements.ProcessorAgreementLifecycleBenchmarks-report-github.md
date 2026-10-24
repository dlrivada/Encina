```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4.968 μs | 0.3700 μs | 0.0203 μs |  1.00 |    3 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.138 μs | 0.9170 μs | 0.0503 μs |  0.63 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.203 μs | 0.1588 μs | 0.0087 μs |  0.24 |    1 | 0.0210 | 0.0191 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.239 μs | 0.4502 μs | 0.0247 μs |  0.25 |    1 | 0.0229 | 0.0210 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.036 μs | 0.3469 μs | 0.0190 μs |  0.21 |    1 | 0.0210 | 0.0191 |     360 B |        0.20 |
