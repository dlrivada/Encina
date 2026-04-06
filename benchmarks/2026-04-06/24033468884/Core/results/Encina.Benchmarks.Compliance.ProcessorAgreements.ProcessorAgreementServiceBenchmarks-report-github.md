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
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,241.8 ns | 32.60 ns |  47.78 ns |  1.00 |    0.05 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,303.4 ns | 28.55 ns |  40.94 ns |  1.05 |    0.05 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,130.2 ns | 85.31 ns | 125.04 ns |  4.14 |    0.19 |    6 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,173.8 ns | 44.58 ns |  65.35 ns |  2.56 |    0.11 |    5 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,219.5 ns | 23.40 ns |  35.02 ns |  0.98 |    0.05 |    3 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,312.6 ns | 18.23 ns |  27.29 ns |  1.06 |    0.05 |    3 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,089.5 ns |  9.35 ns |  14.00 ns |  0.88 |    0.04 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,065.3 ns | 22.38 ns |  33.50 ns |  0.86 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,064.1 ns | 35.50 ns |  49.77 ns |  0.86 |    0.05 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   827.8 ns |  6.26 ns |   9.18 ns |  0.67 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,038.5 ns | 34.19 ns |  51.17 ns |  1.64 |    0.08 |    4 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,033.1 ns | 23.14 ns |  34.64 ns |  0.83 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,019.6 ns | 31.20 ns |  45.73 ns |  0.82 |    0.05 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
