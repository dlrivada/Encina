```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   760.5 ns |    56.48 ns |  3.10 ns |  1.00 |    0.00 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   794.1 ns |   310.09 ns | 17.00 ns |  1.04 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,438.7 ns | 1,091.01 ns | 59.80 ns |  4.52 |    0.07 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,197.1 ns |   545.43 ns | 29.90 ns |  2.89 |    0.04 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   782.5 ns |   207.68 ns | 11.38 ns |  1.03 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   835.2 ns |   200.26 ns | 10.98 ns |  1.10 |    0.01 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   686.0 ns |    90.13 ns |  4.94 ns |  0.90 |    0.01 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   703.4 ns |   319.96 ns | 17.54 ns |  0.92 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   666.2 ns |    53.24 ns |  2.92 ns |  0.88 |    0.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   541.9 ns |   203.10 ns | 11.13 ns |  0.71 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,315.2 ns | 1,434.81 ns | 78.65 ns |  1.73 |    0.09 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   703.6 ns |   586.15 ns | 32.13 ns |  0.93 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   641.7 ns |    53.38 ns |  2.93 ns |  0.84 |    0.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
