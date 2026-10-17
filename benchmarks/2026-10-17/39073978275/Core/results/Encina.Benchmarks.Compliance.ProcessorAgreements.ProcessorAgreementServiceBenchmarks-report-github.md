```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   984.1 ns |   434.58 ns |  23.82 ns |  1.00 |    0.03 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,012.5 ns |   304.16 ns |  16.67 ns |  1.03 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,220.0 ns | 2,280.91 ns | 125.02 ns |  4.29 |    0.14 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,604.4 ns | 1,257.93 ns |  68.95 ns |  2.65 |    0.08 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,009.4 ns |   194.76 ns |  10.68 ns |  1.03 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,076.1 ns |   280.18 ns |  15.36 ns |  1.09 |    0.03 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   907.8 ns |   209.36 ns |  11.48 ns |  0.92 |    0.02 |    1 | 0.0210 | 0.0200 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   869.6 ns |   197.13 ns |  10.81 ns |  0.88 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   842.4 ns |   379.34 ns |  20.79 ns |  0.86 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   710.8 ns |   264.24 ns |  14.48 ns |  0.72 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,635.1 ns |    82.98 ns |   4.55 ns |  1.66 |    0.03 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   875.9 ns |   521.57 ns |  28.59 ns |  0.89 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   819.9 ns |   238.14 ns |  13.05 ns |  0.83 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
