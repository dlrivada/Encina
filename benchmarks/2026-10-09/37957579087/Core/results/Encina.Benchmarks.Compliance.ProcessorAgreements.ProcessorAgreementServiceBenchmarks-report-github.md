```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   673.5 ns |   158.58 ns |   8.69 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   683.1 ns |   118.91 ns |   6.52 ns |  1.01 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,193.6 ns | 2,249.54 ns | 123.30 ns |  4.74 |    0.17 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 1,924.0 ns | 2,451.62 ns | 134.38 ns |  2.86 |    0.18 |    3 | 0.0648 | 0.0324 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   711.4 ns |   242.53 ns |  13.29 ns |  1.06 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   771.8 ns |   182.42 ns |  10.00 ns |  1.15 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   649.9 ns |   129.44 ns |   7.09 ns |  0.97 |    0.01 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   631.0 ns |    78.87 ns |   4.32 ns |  0.94 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   630.2 ns |   721.79 ns |  39.56 ns |  0.94 |    0.05 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   516.2 ns |   141.45 ns |   7.75 ns |  0.77 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,259.0 ns |   228.71 ns |  12.54 ns |  1.87 |    0.03 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   600.3 ns |   238.26 ns |  13.06 ns |  0.89 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   589.9 ns |   192.88 ns |  10.57 ns |  0.88 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
