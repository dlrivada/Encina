```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,143.5 ns |  7.72 ns | 11.55 ns |  1.00 |    0.01 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,156.8 ns | 11.59 ns | 17.00 ns |  1.01 |    0.02 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,952.1 ns | 24.62 ns | 36.08 ns |  4.33 |    0.05 |    7 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,253.3 ns | 39.02 ns | 58.40 ns |  2.85 |    0.06 |    6 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,170.5 ns | 11.71 ns | 17.16 ns |  1.02 |    0.02 |    3 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,238.1 ns | 14.98 ns | 22.42 ns |  1.08 |    0.02 |    4 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,057.3 ns |  9.24 ns | 13.83 ns |  0.92 |    0.02 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,056.1 ns | 17.00 ns | 25.44 ns |  0.92 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,024.4 ns | 17.79 ns | 26.07 ns |  0.90 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   858.3 ns | 13.79 ns | 20.63 ns |  0.75 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,911.9 ns | 15.48 ns | 23.18 ns |  1.67 |    0.03 |    5 | 0.0324 | 0.0305 |     600 B |        1.56 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,016.2 ns | 21.57 ns | 30.24 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   993.4 ns | 13.45 ns | 19.71 ns |  0.87 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
