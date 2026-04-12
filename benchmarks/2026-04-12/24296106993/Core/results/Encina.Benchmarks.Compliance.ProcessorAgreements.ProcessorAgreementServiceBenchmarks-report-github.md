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
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,175.2 ns | 11.33 ns | 16.96 ns |  1.00 |    0.02 |    4 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,187.2 ns | 12.37 ns | 18.51 ns |  1.01 |    0.02 |    4 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,941.4 ns | 14.09 ns | 19.76 ns |  4.21 |    0.06 |    7 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,074.1 ns | 15.88 ns | 23.77 ns |  2.62 |    0.04 |    6 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,231.0 ns | 21.66 ns | 32.42 ns |  1.05 |    0.03 |    4 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,283.6 ns | 14.60 ns | 21.86 ns |  1.09 |    0.02 |    4 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,092.5 ns | 15.87 ns | 23.75 ns |  0.93 |    0.02 |    3 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,035.6 ns | 10.66 ns | 15.95 ns |  0.88 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,001.9 ns | 11.88 ns | 17.79 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   842.2 ns |  6.53 ns |  9.57 ns |  0.72 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,926.1 ns | 13.57 ns | 19.46 ns |  1.64 |    0.03 |    5 | 0.0324 | 0.0305 |     600 B |        1.56 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,000.9 ns | 12.20 ns | 18.26 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   978.0 ns | 11.83 ns | 17.70 ns |  0.83 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
