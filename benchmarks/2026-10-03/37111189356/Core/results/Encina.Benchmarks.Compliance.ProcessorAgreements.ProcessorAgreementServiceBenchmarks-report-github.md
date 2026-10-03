```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,202.7 ns |   358.54 ns | 19.65 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,229.3 ns |   709.04 ns | 38.86 ns |  1.02 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,048.3 ns | 1,394.82 ns | 76.45 ns |  4.20 |    0.08 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,141.5 ns | 1,284.49 ns | 70.41 ns |  2.61 |    0.06 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,187.9 ns |    99.04 ns |  5.43 ns |  0.99 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,238.8 ns |   216.04 ns | 11.84 ns |  1.03 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,075.1 ns |   238.12 ns | 13.05 ns |  0.89 |    0.02 |    1 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,063.7 ns |   373.75 ns | 20.49 ns |  0.88 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   992.3 ns |   307.46 ns | 16.85 ns |  0.83 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   892.5 ns |   133.17 ns |  7.30 ns |  0.74 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,016.3 ns |   116.07 ns |  6.36 ns |  1.68 |    0.02 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,007.8 ns |   624.56 ns | 34.23 ns |  0.84 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   971.6 ns |   335.57 ns | 18.39 ns |  0.81 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
