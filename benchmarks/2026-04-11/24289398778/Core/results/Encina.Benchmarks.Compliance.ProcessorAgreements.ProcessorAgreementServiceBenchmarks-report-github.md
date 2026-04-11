```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,242.5 ns | 15.38 ns | 23.03 ns |  1.00 |    0.03 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,255.1 ns | 17.83 ns | 26.68 ns |  1.01 |    0.03 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,063.0 ns | 46.50 ns | 66.69 ns |  4.08 |    0.09 |    7 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,260.1 ns | 50.70 ns | 75.88 ns |  2.62 |    0.08 |    6 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,259.6 ns | 27.48 ns | 41.12 ns |  1.01 |    0.04 |    3 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,342.6 ns | 27.91 ns | 41.77 ns |  1.08 |    0.04 |    4 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,174.6 ns | 23.98 ns | 35.15 ns |  0.95 |    0.03 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,080.3 ns | 21.77 ns | 32.58 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,093.7 ns | 24.43 ns | 36.56 ns |  0.88 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   894.4 ns | 19.36 ns | 28.98 ns |  0.72 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,104.6 ns | 19.74 ns | 29.55 ns |  1.69 |    0.04 |    5 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,105.3 ns | 24.34 ns | 36.43 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,071.4 ns | 28.46 ns | 42.60 ns |  0.86 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
