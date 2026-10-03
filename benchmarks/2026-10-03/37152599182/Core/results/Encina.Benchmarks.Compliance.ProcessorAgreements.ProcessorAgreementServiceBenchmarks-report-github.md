```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   990.8 ns |   393.51 ns | 21.57 ns |  1.00 |    0.03 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,019.3 ns |   411.35 ns | 22.55 ns |  1.03 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,190.9 ns | 1,238.62 ns | 67.89 ns |  4.23 |    0.10 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,476.8 ns | 1,146.75 ns | 62.86 ns |  2.50 |    0.07 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,009.5 ns |    87.48 ns |  4.80 ns |  1.02 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,042.2 ns |   235.08 ns | 12.89 ns |  1.05 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   881.9 ns |    94.81 ns |  5.20 ns |  0.89 |    0.02 |    1 | 0.0210 | 0.0200 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   863.7 ns |   245.19 ns | 13.44 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   804.7 ns |    72.20 ns |  3.96 ns |  0.81 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   671.0 ns |   169.67 ns |  9.30 ns |  0.68 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,615.0 ns | 1,185.62 ns | 64.99 ns |  1.63 |    0.06 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   817.7 ns |   100.62 ns |  5.52 ns |  0.83 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   796.4 ns |   286.28 ns | 15.69 ns |  0.80 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
