```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 5.105 μs | 0.0646 μs | 0.0967 μs |  1.00 |    0.03 |    5 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.138 μs | 0.0416 μs | 0.0610 μs |  0.61 |    0.02 |    4 | 0.0648 | 0.0305 |    1168 B |        0.66 |
| &#39;AuditDPA (record audit)&#39;      | 1.240 μs | 0.0272 μs | 0.0390 μs |  0.24 |    0.01 |    2 | 0.0210 | 0.0191 |     400 B |        0.23 |
| &#39;RenewDPA (extend expiration)&#39; | 1.341 μs | 0.0422 μs | 0.0632 μs |  0.26 |    0.01 |    3 | 0.0229 | 0.0210 |     424 B |        0.24 |
| &#39;TerminateDPA (end agreement)&#39; | 1.140 μs | 0.0222 μs | 0.0326 μs |  0.22 |    0.01 |    1 | 0.0210 | 0.0191 |     392 B |        0.22 |
