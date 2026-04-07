```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,212.0 ns |  9.41 ns | 13.79 ns |  1.00 |    0.02 |    5 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,266.6 ns | 14.18 ns | 21.22 ns |  1.05 |    0.02 |    5 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,146.7 ns | 35.16 ns | 51.53 ns |  4.25 |    0.06 |    8 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,291.7 ns | 45.22 ns | 66.29 ns |  2.72 |    0.06 |    7 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,231.5 ns | 18.43 ns | 27.01 ns |  1.02 |    0.02 |    5 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,268.4 ns | 30.30 ns | 43.45 ns |  1.05 |    0.04 |    5 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,139.1 ns | 24.78 ns | 36.32 ns |  0.94 |    0.03 |    4 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,047.0 ns | 17.83 ns | 26.68 ns |  0.86 |    0.02 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,056.3 ns | 20.15 ns | 29.53 ns |  0.87 |    0.03 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   838.6 ns | 18.43 ns | 27.01 ns |  0.69 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,961.9 ns | 32.91 ns | 49.26 ns |  1.62 |    0.04 |    6 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         |   986.5 ns | 13.91 ns | 19.95 ns |  0.81 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   970.5 ns | 17.79 ns | 26.63 ns |  0.80 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
