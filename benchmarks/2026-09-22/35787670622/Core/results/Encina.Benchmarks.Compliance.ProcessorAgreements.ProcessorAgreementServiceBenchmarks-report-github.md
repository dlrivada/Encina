```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,144.5 ns | 324.2 ns | 17.77 ns |  1.00 |    0.02 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,161.4 ns | 522.8 ns | 28.66 ns |  1.01 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,016.9 ns | 301.5 ns | 16.53 ns |  4.38 |    0.06 |    5 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,003.7 ns | 110.7 ns |  6.07 ns |  2.62 |    0.04 |    4 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,139.2 ns | 153.5 ns |  8.41 ns |  1.00 |    0.01 |    2 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,190.2 ns | 343.4 ns | 18.82 ns |  1.04 |    0.02 |    2 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,057.2 ns | 155.1 ns |  8.50 ns |  0.92 |    0.01 |    2 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,079.1 ns | 487.5 ns | 26.72 ns |  0.94 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,021.7 ns | 575.1 ns | 31.52 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   817.9 ns | 171.5 ns |  9.40 ns |  0.71 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,920.0 ns | 647.5 ns | 35.49 ns |  1.68 |    0.03 |    3 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   988.9 ns | 203.7 ns | 11.17 ns |  0.86 |    0.01 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   985.3 ns | 292.2 ns | 16.01 ns |  0.86 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
