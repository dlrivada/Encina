```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,169.6 ns | 490.2 ns | 26.87 ns |  1.00 |    0.03 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,225.2 ns | 387.4 ns | 21.23 ns |  1.05 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,193.1 ns | 975.8 ns | 53.49 ns |  4.44 |    0.10 |    5 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,213.0 ns | 437.7 ns | 23.99 ns |  2.75 |    0.06 |    4 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,197.8 ns | 114.5 ns |  6.28 ns |  1.02 |    0.02 |    2 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,240.1 ns | 256.4 ns | 14.06 ns |  1.06 |    0.02 |    2 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,109.6 ns | 281.8 ns | 15.45 ns |  0.95 |    0.02 |    2 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,124.9 ns | 524.1 ns | 28.73 ns |  0.96 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,091.6 ns | 170.8 ns |  9.36 ns |  0.93 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   873.6 ns | 378.4 ns | 20.74 ns |  0.75 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,144.3 ns | 721.9 ns | 39.57 ns |  1.83 |    0.05 |    3 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,085.1 ns | 195.2 ns | 10.70 ns |  0.93 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,073.6 ns | 739.7 ns | 40.55 ns |  0.92 |    0.04 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
