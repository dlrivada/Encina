```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,238.7 ns |  20.70 ns |  30.98 ns |  1.00 |    0.03 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,268.9 ns |  38.30 ns |  57.32 ns |  1.03 |    0.05 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,119.3 ns | 114.71 ns | 171.69 ns |  4.14 |    0.17 |    6 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,396.9 ns | 110.99 ns | 166.13 ns |  2.74 |    0.15 |    5 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,338.7 ns |  37.79 ns |  56.57 ns |  1.08 |    0.05 |    3 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,400.4 ns |  38.76 ns |  56.81 ns |  1.13 |    0.05 |    3 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,155.9 ns |  24.09 ns |  36.06 ns |  0.93 |    0.04 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,165.2 ns |  31.69 ns |  46.45 ns |  0.94 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,121.5 ns |  46.71 ns |  69.91 ns |  0.91 |    0.06 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   920.5 ns |  29.29 ns |  43.83 ns |  0.74 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,071.4 ns |  40.01 ns |  58.65 ns |  1.67 |    0.06 |    4 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,107.0 ns |  28.96 ns |  43.34 ns |  0.89 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,073.5 ns |  30.51 ns |  45.66 ns |  0.87 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
