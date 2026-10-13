```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   936.0 ns |   115.66 ns |  6.34 ns |  1.00 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   933.4 ns |   283.24 ns | 15.53 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.15 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,987.2 ns | 1,223.99 ns | 67.09 ns |  4.26 |    0.07 |    4 | 0.0992 | 0.0458 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,445.2 ns |   189.98 ns | 10.41 ns |  2.61 |    0.02 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   986.1 ns |   124.39 ns |  6.82 ns |  1.05 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        1.00 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,012.8 ns |   385.33 ns | 21.12 ns |  1.08 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.07 |
| &#39;TerminateDPA (end agreement)&#39;             |   872.8 ns |    40.53 ns |  2.22 ns |  0.93 |    0.01 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   870.4 ns |   175.74 ns |  9.63 ns |  0.93 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   805.1 ns |   123.17 ns |  6.75 ns |  0.86 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   687.3 ns |   248.62 ns | 13.63 ns |  0.73 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,585.4 ns |   330.81 ns | 18.13 ns |  1.69 |    0.02 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   841.2 ns |   107.69 ns |  5.90 ns |  0.90 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   800.2 ns |   108.30 ns |  5.94 ns |  0.85 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
