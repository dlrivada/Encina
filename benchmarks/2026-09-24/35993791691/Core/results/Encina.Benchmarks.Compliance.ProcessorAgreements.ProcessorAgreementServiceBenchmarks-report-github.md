```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   757.9 ns | 609.14 ns | 33.39 ns |  1.00 |    0.05 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   744.4 ns | 101.76 ns |  5.58 ns |  0.98 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,257.5 ns | 385.27 ns | 21.12 ns |  4.30 |    0.16 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,050.2 ns | 509.88 ns | 27.95 ns |  2.71 |    0.11 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   735.3 ns | 453.32 ns | 24.85 ns |  0.97 |    0.05 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   795.3 ns | 240.80 ns | 13.20 ns |  1.05 |    0.04 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   647.8 ns | 597.38 ns | 32.74 ns |  0.86 |    0.05 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   681.4 ns | 946.32 ns | 51.87 ns |  0.90 |    0.07 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   610.1 ns | 317.53 ns | 17.40 ns |  0.81 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   495.2 ns | 329.58 ns | 18.07 ns |  0.65 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,204.7 ns | 761.18 ns | 41.72 ns |  1.59 |    0.08 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   597.7 ns | 104.29 ns |  5.72 ns |  0.79 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   548.2 ns |  83.64 ns |  4.58 ns |  0.72 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
