```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,355.2 ns | 1,486.1 ns |  81.46 ns |  1.00 |    0.07 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,380.8 ns | 1,671.1 ns |  91.60 ns |  1.02 |    0.08 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,455.8 ns |   904.7 ns |  49.59 ns |  4.04 |    0.22 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,605.2 ns | 2,152.8 ns | 118.00 ns |  2.67 |    0.16 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,401.7 ns | 2,009.8 ns | 110.16 ns |  1.04 |    0.09 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,499.4 ns | 1,621.5 ns |  88.88 ns |  1.11 |    0.08 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,118.7 ns |   758.5 ns |  41.58 ns |  0.83 |    0.05 |    1 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,292.5 ns |   558.1 ns |  30.59 ns |  0.96 |    0.05 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,192.7 ns | 1,569.7 ns |  86.04 ns |  0.88 |    0.07 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   956.0 ns |   703.4 ns |  38.55 ns |  0.71 |    0.05 |    1 | 0.0229 | 0.0210 |     384 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,147.7 ns | 1,925.4 ns | 105.54 ns |  1.59 |    0.11 |    2 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,144.5 ns |   935.0 ns |  51.25 ns |  0.85 |    0.06 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,090.6 ns | 1,105.2 ns |  60.58 ns |  0.81 |    0.06 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
