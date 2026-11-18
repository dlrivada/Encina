```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 5.160 μs | 0.9181 μs | 0.0503 μs |  1.00 |    3 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.039 μs | 0.8172 μs | 0.0448 μs |  0.59 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.184 μs | 0.3932 μs | 0.0216 μs |  0.23 |    1 | 0.0210 | 0.0191 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.240 μs | 0.1589 μs | 0.0087 μs |  0.24 |    1 | 0.0229 | 0.0210 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.126 μs | 0.4000 μs | 0.0219 μs |  0.22 |    1 | 0.0210 | 0.0191 |     360 B |        0.20 |
