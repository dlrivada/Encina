```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,145.2 ns | 348.35 ns | 19.09 ns |  1.00 |    0.02 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,173.8 ns | 382.53 ns | 20.97 ns |  1.03 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,935.2 ns | 684.62 ns | 37.53 ns |  4.31 |    0.07 |    5 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,014.8 ns | 100.77 ns |  5.52 ns |  2.63 |    0.04 |    4 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,162.8 ns | 381.00 ns | 20.88 ns |  1.02 |    0.02 |    2 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,203.1 ns | 206.38 ns | 11.31 ns |  1.05 |    0.02 |    2 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,047.1 ns | 282.05 ns | 15.46 ns |  0.91 |    0.02 |    2 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,004.1 ns | 389.99 ns | 21.38 ns |  0.88 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   986.2 ns | 315.48 ns | 17.29 ns |  0.86 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   807.4 ns | 193.97 ns | 10.63 ns |  0.71 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,912.4 ns |  82.53 ns |  4.52 ns |  1.67 |    0.02 |    3 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,032.1 ns | 301.44 ns | 16.52 ns |  0.90 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   993.4 ns | 689.25 ns | 37.78 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
