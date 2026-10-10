```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   756.3 ns |   433.62 ns | 23.77 ns |  1.00 |    0.04 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   736.9 ns |   577.60 ns | 31.66 ns |  0.97 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,205.6 ns | 1,438.93 ns | 78.87 ns |  4.24 |    0.15 |    4 | 0.1030 | 0.0496 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,088.9 ns | 1,168.55 ns | 64.05 ns |  2.76 |    0.10 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  |   740.9 ns |   310.31 ns | 17.01 ns |  0.98 |    0.03 |    1 | 0.0219 | 0.0210 |     384 B |        1.09 |
| &#39;RenewDPA (extend expiration)&#39;             |   780.0 ns |   260.10 ns | 14.26 ns |  1.03 |    0.03 |    1 | 0.0229 | 0.0219 |     408 B |        1.16 |
| &#39;TerminateDPA (end agreement)&#39;             |   674.6 ns |    61.40 ns |  3.37 ns |  0.89 |    0.02 |    1 | 0.0210 | 0.0200 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   644.6 ns |   242.44 ns | 13.29 ns |  0.85 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   597.2 ns |   189.21 ns | 10.37 ns |  0.79 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   515.5 ns |   193.66 ns | 10.62 ns |  0.68 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,186.6 ns |   529.84 ns | 29.04 ns |  1.57 |    0.05 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   606.7 ns |   308.11 ns | 16.89 ns |  0.80 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   634.9 ns |   418.69 ns | 22.95 ns |  0.84 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
