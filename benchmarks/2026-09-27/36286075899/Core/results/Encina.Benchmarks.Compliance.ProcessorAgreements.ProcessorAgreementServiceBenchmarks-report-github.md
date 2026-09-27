```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,193.0 ns |  8.63 ns | 12.65 ns |  1.00 |    0.01 |    4 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,217.3 ns | 12.77 ns | 19.12 ns |  1.02 |    0.02 |    4 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,916.6 ns | 29.17 ns | 43.66 ns |  4.12 |    0.06 |    7 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,131.1 ns | 26.23 ns | 38.45 ns |  2.62 |    0.04 |    6 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,211.7 ns | 13.84 ns | 20.72 ns |  1.02 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,264.6 ns | 13.11 ns | 19.63 ns |  1.06 |    0.02 |    4 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,129.5 ns | 12.98 ns | 19.03 ns |  0.95 |    0.02 |    3 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,079.5 ns | 19.11 ns | 28.60 ns |  0.90 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,070.5 ns | 21.67 ns | 32.43 ns |  0.90 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   849.6 ns | 14.36 ns | 21.49 ns |  0.71 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,967.4 ns | 17.76 ns | 26.58 ns |  1.65 |    0.03 |    5 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,059.4 ns | 20.03 ns | 29.98 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,008.5 ns | 15.30 ns | 22.90 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
