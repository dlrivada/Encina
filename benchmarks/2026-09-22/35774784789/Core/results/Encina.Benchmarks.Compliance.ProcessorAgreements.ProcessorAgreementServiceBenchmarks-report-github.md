```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   954.2 ns | 514.93 ns | 28.22 ns |  1.00 |    0.04 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   995.1 ns | 993.08 ns | 54.43 ns |  1.04 |    0.06 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,175.9 ns | 906.70 ns | 49.70 ns |  4.38 |    0.12 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,529.0 ns | 230.84 ns | 12.65 ns |  2.65 |    0.07 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  |   983.1 ns | 313.00 ns | 17.16 ns |  1.03 |    0.03 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,037.5 ns | 477.16 ns | 26.15 ns |  1.09 |    0.04 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   909.9 ns |  92.40 ns |  5.06 ns |  0.95 |    0.02 |    1 | 0.0210 | 0.0200 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   868.4 ns | 235.06 ns | 12.88 ns |  0.91 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   832.2 ns | 133.41 ns |  7.31 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   701.3 ns | 112.07 ns |  6.14 ns |  0.74 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,617.5 ns |  90.94 ns |  4.98 ns |  1.70 |    0.04 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   850.7 ns | 116.13 ns |  6.37 ns |  0.89 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   864.0 ns | 321.24 ns | 17.61 ns |  0.91 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
