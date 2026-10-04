```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,185.9 ns | 17.22 ns |  25.24 ns |  1.00 |    0.03 |    4 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,260.5 ns | 14.75 ns |  21.16 ns |  1.06 |    0.03 |    4 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,229.3 ns | 76.54 ns | 114.55 ns |  4.41 |    0.13 |    7 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,131.0 ns | 29.42 ns |  41.24 ns |  2.64 |    0.07 |    6 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,232.3 ns | 26.16 ns |  39.16 ns |  1.04 |    0.04 |    4 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,291.6 ns | 22.07 ns |  32.35 ns |  1.09 |    0.04 |    4 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,121.4 ns | 19.91 ns |  29.80 ns |  0.95 |    0.03 |    3 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,049.5 ns | 19.74 ns |  29.54 ns |  0.89 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,011.1 ns | 20.38 ns |  30.51 ns |  0.85 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   838.6 ns |  8.77 ns |  12.85 ns |  0.71 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,982.0 ns | 47.30 ns |  67.84 ns |  1.67 |    0.07 |    5 | 0.0324 | 0.0305 |     600 B |        1.56 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,046.7 ns | 21.28 ns |  30.52 ns |  0.88 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,019.3 ns | 26.07 ns |  38.21 ns |  0.86 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
