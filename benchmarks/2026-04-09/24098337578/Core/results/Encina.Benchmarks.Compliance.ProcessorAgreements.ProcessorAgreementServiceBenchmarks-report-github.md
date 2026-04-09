```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,247.4 ns |  9.83 ns |  14.10 ns |  1.00 |    0.02 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,294.6 ns | 15.18 ns |  22.73 ns |  1.04 |    0.02 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,286.0 ns | 85.09 ns | 124.73 ns |  4.24 |    0.11 |    6 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,221.3 ns | 26.42 ns |  38.72 ns |  2.58 |    0.04 |    5 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,241.1 ns | 11.18 ns |  16.38 ns |  1.00 |    0.02 |    3 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,341.1 ns | 14.98 ns |  22.42 ns |  1.08 |    0.02 |    3 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,154.3 ns | 13.54 ns |  20.27 ns |  0.93 |    0.02 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,107.5 ns | 16.35 ns |  24.48 ns |  0.89 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,102.4 ns | 16.31 ns |  24.41 ns |  0.88 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   916.0 ns |  9.55 ns |  14.00 ns |  0.73 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,052.9 ns | 19.83 ns |  29.68 ns |  1.65 |    0.03 |    4 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,078.6 ns | 17.54 ns |  26.25 ns |  0.86 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,050.9 ns | 18.71 ns |  27.42 ns |  0.84 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
