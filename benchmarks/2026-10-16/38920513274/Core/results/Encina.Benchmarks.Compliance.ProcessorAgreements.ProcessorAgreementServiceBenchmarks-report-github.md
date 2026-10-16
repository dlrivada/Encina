```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   757.2 ns |   512.76 ns | 28.11 ns |  1.00 |    0.05 |    2 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   766.6 ns |    17.18 ns |  0.94 ns |  1.01 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,371.7 ns | 1,234.45 ns | 67.66 ns |  4.46 |    0.17 |    5 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,101.2 ns |   967.83 ns | 53.05 ns |  2.78 |    0.11 |    4 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   796.5 ns |   589.95 ns | 32.34 ns |  1.05 |    0.05 |    2 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   790.0 ns |   108.22 ns |  5.93 ns |  1.04 |    0.03 |    2 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   669.0 ns |    54.53 ns |  2.99 ns |  0.88 |    0.03 |    2 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   675.5 ns |   159.80 ns |  8.76 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   672.5 ns |   382.37 ns | 20.96 ns |  0.89 |    0.04 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   487.7 ns |   259.59 ns | 14.23 ns |  0.64 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,188.3 ns |   613.36 ns | 33.62 ns |  1.57 |    0.06 |    3 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   592.7 ns |    42.41 ns |  2.32 ns |  0.78 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   609.8 ns |   159.13 ns |  8.72 ns |  0.81 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
