```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,134.6 ns |   291.9 ns | 16.00 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,178.5 ns |   390.7 ns | 21.41 ns |  1.04 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,938.0 ns |   374.6 ns | 20.53 ns |  4.35 |    0.06 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,126.4 ns | 1,236.5 ns | 67.77 ns |  2.76 |    0.06 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,171.7 ns |   488.9 ns | 26.80 ns |  1.03 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,242.1 ns |   500.2 ns | 27.42 ns |  1.09 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,065.3 ns |   229.0 ns | 12.55 ns |  0.94 |    0.01 |    1 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,019.7 ns |   297.0 ns | 16.28 ns |  0.90 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   990.5 ns |   454.1 ns | 24.89 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   838.1 ns |   272.6 ns | 14.94 ns |  0.74 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,916.9 ns |   203.1 ns | 11.13 ns |  1.69 |    0.02 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   992.6 ns |   344.3 ns | 18.87 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   980.3 ns |   572.0 ns | 31.36 ns |  0.86 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
