```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   949.7 ns |   351.08 ns | 19.24 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   972.4 ns |   129.99 ns |  7.13 ns |  1.02 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,196.1 ns | 1,433.78 ns | 78.59 ns |  4.42 |    0.11 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,573.4 ns | 1,147.97 ns | 62.92 ns |  2.71 |    0.07 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,003.4 ns |    10.81 ns |  0.59 ns |  1.06 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,062.7 ns |   323.30 ns | 17.72 ns |  1.12 |    0.03 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   924.1 ns |   704.92 ns | 38.64 ns |  0.97 |    0.04 |    1 | 0.0210 | 0.0200 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   929.5 ns |   445.92 ns | 24.44 ns |  0.98 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   900.0 ns |   140.98 ns |  7.73 ns |  0.95 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   739.6 ns |   154.66 ns |  8.48 ns |  0.78 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,733.5 ns |   141.80 ns |  7.77 ns |  1.83 |    0.03 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   930.6 ns |   558.99 ns | 30.64 ns |  0.98 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   825.6 ns |   819.72 ns | 44.93 ns |  0.87 |    0.04 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
