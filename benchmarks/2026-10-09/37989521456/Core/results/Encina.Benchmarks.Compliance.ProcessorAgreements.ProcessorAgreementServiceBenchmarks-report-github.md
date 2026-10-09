```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,206.9 ns | 2,733.5 ns | 149.83 ns |  1.01 |    0.15 |    2 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,105.3 ns |   861.5 ns |  47.22 ns |  0.92 |    0.10 |    2 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,238.9 ns | 3,710.3 ns | 203.37 ns |  3.55 |    0.40 |    5 | 0.0153 | 0.0076 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,979.4 ns |   257.5 ns |  14.11 ns |  2.49 |    0.26 |    4 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,148.0 ns | 1,780.5 ns |  97.60 ns |  0.96 |    0.12 |    2 | 0.0038 | 0.0019 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,288.1 ns | 1,953.6 ns | 107.08 ns |  1.08 |    0.14 |    2 | 0.0038 | 0.0019 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,170.2 ns | 1,980.8 ns | 108.57 ns |  0.98 |    0.13 |    2 | 0.0038 | 0.0019 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,155.2 ns |   766.0 ns |  41.99 ns |  0.97 |    0.11 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,122.7 ns |   275.0 ns |  15.07 ns |  0.94 |    0.10 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   892.8 ns |   560.5 ns |  30.72 ns |  0.75 |    0.08 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,776.0 ns |   207.1 ns |  11.35 ns |  1.49 |    0.15 |    3 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,130.3 ns |   166.0 ns |   9.10 ns |  0.95 |    0.10 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,097.9 ns |   215.8 ns |  11.83 ns |  0.92 |    0.10 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
