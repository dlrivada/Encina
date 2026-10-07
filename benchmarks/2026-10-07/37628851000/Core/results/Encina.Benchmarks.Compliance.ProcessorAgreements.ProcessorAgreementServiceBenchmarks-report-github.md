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
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   739.7 ns |   127.84 ns |   7.01 ns |  1.00 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   735.0 ns |   286.28 ns |  15.69 ns |  0.99 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,167.6 ns | 1,268.78 ns |  69.55 ns |  4.28 |    0.09 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,051.7 ns | 2,364.66 ns | 129.62 ns |  2.77 |    0.15 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   786.7 ns |   289.28 ns |  15.86 ns |  1.06 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   804.5 ns |   277.28 ns |  15.20 ns |  1.09 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   692.0 ns |   150.82 ns |   8.27 ns |  0.94 |    0.01 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   649.4 ns |   803.42 ns |  44.04 ns |  0.88 |    0.05 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   584.9 ns |    62.96 ns |   3.45 ns |  0.79 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   533.0 ns |   204.56 ns |  11.21 ns |  0.72 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,280.9 ns |   221.81 ns |  12.16 ns |  1.73 |    0.02 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   635.7 ns |   591.09 ns |  32.40 ns |  0.86 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   618.0 ns |   437.26 ns |  23.97 ns |  0.84 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
