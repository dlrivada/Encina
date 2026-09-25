```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,183.4 ns |   346.8 ns | 19.01 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,224.2 ns |   277.8 ns | 15.23 ns |  1.03 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,106.2 ns | 1,096.6 ns | 60.11 ns |  4.32 |    0.07 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,151.3 ns |   750.3 ns | 41.12 ns |  2.66 |    0.05 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,197.6 ns |   125.2 ns |  6.86 ns |  1.01 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,286.6 ns |   363.6 ns | 19.93 ns |  1.09 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,104.2 ns |   246.4 ns | 13.51 ns |  0.93 |    0.02 |    1 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,068.9 ns |   394.7 ns | 21.64 ns |  0.90 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,034.0 ns |   448.0 ns | 24.56 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   862.5 ns |   261.9 ns | 14.36 ns |  0.73 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,971.0 ns |   343.6 ns | 18.83 ns |  1.67 |    0.03 |    2 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,045.9 ns |   309.1 ns | 16.94 ns |  0.88 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,019.2 ns |   453.5 ns | 24.86 ns |  0.86 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
