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
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   947.1 ns |   327.71 ns |  17.96 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   988.8 ns |    94.91 ns |   5.20 ns |  1.04 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.15 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,056.4 ns | 1,524.55 ns |  83.57 ns |  4.28 |    0.10 |    4 | 0.0992 | 0.0458 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,508.3 ns | 1,828.39 ns | 100.22 ns |  2.65 |    0.10 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   982.0 ns |   337.24 ns |  18.49 ns |  1.04 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.00 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,057.3 ns |   261.20 ns |  14.32 ns |  1.12 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.07 |
| &#39;TerminateDPA (end agreement)&#39;             |   976.6 ns |   301.09 ns |  16.50 ns |  1.03 |    0.02 |    1 | 0.0210 | 0.0191 |     360 B |        0.98 |
| &#39;GetDPA by ID (cached read)&#39;               |   930.2 ns | 1,097.83 ns |  60.18 ns |  0.98 |    0.06 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   887.0 ns | 1,226.46 ns |  67.23 ns |  0.94 |    0.06 |    1 | 0.0248 | 0.0229 |     424 B |        1.15 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   750.6 ns |   130.58 ns |   7.16 ns |  0.79 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,667.7 ns |   800.12 ns |  43.86 ns |  1.76 |    0.05 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   889.8 ns |   471.79 ns |  25.86 ns |  0.94 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   869.5 ns |    52.66 ns |   2.89 ns |  0.92 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
