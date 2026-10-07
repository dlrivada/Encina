```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,194.6 ns | 456.81 ns | 25.04 ns |  1.00 |    0.03 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,259.9 ns | 176.88 ns |  9.70 ns |  1.06 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,115.9 ns | 656.51 ns | 35.99 ns |  4.28 |    0.08 |    5 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,207.5 ns | 705.10 ns | 38.65 ns |  2.69 |    0.06 |    4 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,194.9 ns | 258.84 ns | 14.19 ns |  1.00 |    0.02 |    2 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,283.3 ns | 249.76 ns | 13.69 ns |  1.07 |    0.02 |    2 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,149.6 ns | 250.38 ns | 13.72 ns |  0.96 |    0.02 |    2 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,127.1 ns | 570.91 ns | 31.29 ns |  0.94 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,062.6 ns | 655.24 ns | 35.92 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   869.0 ns |  43.65 ns |  2.39 ns |  0.73 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,030.2 ns | 741.03 ns | 40.62 ns |  1.70 |    0.04 |    3 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,047.9 ns | 423.06 ns | 23.19 ns |  0.88 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,061.7 ns | 486.45 ns | 26.66 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
