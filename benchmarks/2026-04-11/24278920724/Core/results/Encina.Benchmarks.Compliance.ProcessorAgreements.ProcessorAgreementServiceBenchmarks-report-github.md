```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,224.8 ns |  21.30 ns |  30.55 ns |  1.00 |    0.03 |    2 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,201.0 ns |  11.45 ns |  16.42 ns |  0.98 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,078.2 ns |  13.49 ns |  18.92 ns |  4.15 |    0.10 |    6 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,175.0 ns |  23.29 ns |  34.14 ns |  2.59 |    0.07 |    5 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,267.6 ns |  14.77 ns |  21.65 ns |  1.04 |    0.03 |    2 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,382.4 ns |  36.35 ns |  54.41 ns |  1.13 |    0.05 |    3 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,169.7 ns |  26.14 ns |  39.13 ns |  0.96 |    0.04 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,070.9 ns |  13.05 ns |  18.72 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,061.0 ns |  22.97 ns |  33.67 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   881.6 ns |  21.21 ns |  31.09 ns |  0.72 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,085.5 ns |  23.41 ns |  33.57 ns |  1.70 |    0.05 |    4 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,148.0 ns | 113.53 ns | 169.93 ns |  0.94 |    0.14 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,104.0 ns |  64.05 ns |  89.79 ns |  0.90 |    0.08 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
