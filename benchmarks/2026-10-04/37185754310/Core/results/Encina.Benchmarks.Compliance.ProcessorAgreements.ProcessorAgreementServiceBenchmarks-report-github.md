```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,255.6 ns | 1,029.7 ns | 56.44 ns |  1.00 |    0.05 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,344.3 ns |   815.5 ns | 44.70 ns |  1.07 |    0.05 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,326.6 ns | 1,704.9 ns | 93.45 ns |  4.25 |    0.18 |    5 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,498.5 ns |   546.3 ns | 29.95 ns |  2.79 |    0.11 |    4 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,260.5 ns |   140.0 ns |  7.68 ns |  1.01 |    0.04 |    2 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,317.7 ns |   268.5 ns | 14.72 ns |  1.05 |    0.04 |    2 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,171.5 ns |   350.2 ns | 19.20 ns |  0.93 |    0.04 |    2 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,102.8 ns |   637.8 ns | 34.96 ns |  0.88 |    0.04 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,102.7 ns |   706.7 ns | 38.73 ns |  0.88 |    0.04 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   856.4 ns |   172.6 ns |  9.46 ns |  0.68 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,951.0 ns |   301.0 ns | 16.50 ns |  1.56 |    0.06 |    3 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,062.2 ns |   743.8 ns | 40.77 ns |  0.85 |    0.04 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,029.7 ns |   426.3 ns | 23.37 ns |  0.82 |    0.04 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
