```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   734.3 ns |   494.7 ns |  27.12 ns |  1.00 |    0.05 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   748.8 ns |   237.0 ns |  12.99 ns |  1.02 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,291.7 ns | 1,383.2 ns |  75.82 ns |  4.49 |    0.17 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,127.0 ns | 4,192.5 ns | 229.81 ns |  2.90 |    0.29 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   778.5 ns |   563.8 ns |  30.90 ns |  1.06 |    0.05 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   763.0 ns |   146.9 ns |   8.05 ns |  1.04 |    0.03 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   687.1 ns |   487.2 ns |  26.71 ns |  0.94 |    0.04 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   593.2 ns |   691.3 ns |  37.89 ns |  0.81 |    0.05 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   651.0 ns |   304.3 ns |  16.68 ns |  0.89 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   511.8 ns |   215.2 ns |  11.80 ns |  0.70 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,355.9 ns |   969.2 ns |  53.12 ns |  1.85 |    0.09 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   695.3 ns |   233.9 ns |  12.82 ns |  0.95 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   691.6 ns |   520.1 ns |  28.51 ns |  0.94 |    0.05 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
