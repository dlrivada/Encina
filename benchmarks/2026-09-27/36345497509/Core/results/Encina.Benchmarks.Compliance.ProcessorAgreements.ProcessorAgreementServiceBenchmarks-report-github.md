```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   946.7 ns | 352.7 ns | 19.33 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   979.4 ns | 607.9 ns | 33.32 ns |  1.03 |    0.04 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,010.5 ns | 515.7 ns | 28.27 ns |  4.24 |    0.08 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,414.1 ns | 777.7 ns | 42.63 ns |  2.55 |    0.06 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  |   966.2 ns | 261.4 ns | 14.33 ns |  1.02 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,035.9 ns | 601.7 ns | 32.98 ns |  1.09 |    0.04 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   868.1 ns | 216.4 ns | 11.86 ns |  0.92 |    0.02 |    1 | 0.0210 | 0.0200 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   863.7 ns | 278.8 ns | 15.28 ns |  0.91 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   792.0 ns | 175.4 ns |  9.62 ns |  0.84 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   694.7 ns | 348.4 ns | 19.10 ns |  0.73 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,652.8 ns | 494.8 ns | 27.12 ns |  1.75 |    0.04 |    2 | 0.0324 | 0.0305 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   863.4 ns | 655.7 ns | 35.94 ns |  0.91 |    0.04 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   828.4 ns | 169.5 ns |  9.29 ns |  0.88 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
