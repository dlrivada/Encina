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
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,098.8 ns | 2,450.7 ns | 134.33 ns |  1.01 |    0.15 |    2 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   947.4 ns |   610.2 ns |  33.45 ns |  0.87 |    0.10 |    2 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,805.2 ns | 2,404.5 ns | 131.80 ns |  3.50 |    0.39 |    5 | 0.0153 | 0.0076 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,650.6 ns | 3,215.9 ns | 176.28 ns |  2.44 |    0.30 |    4 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,170.1 ns |   352.2 ns |  19.30 ns |  1.08 |    0.12 |    2 | 0.0038 | 0.0029 |     384 B |        1.09 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,064.0 ns |   965.3 ns |  52.91 ns |  0.98 |    0.11 |    2 | 0.0038 | 0.0019 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,060.2 ns |   712.8 ns |  39.07 ns |  0.97 |    0.11 |    2 | 0.0038 | 0.0029 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,083.4 ns |   358.6 ns |  19.66 ns |  1.00 |    0.11 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             |   948.8 ns | 1,080.3 ns |  59.21 ns |  0.87 |    0.10 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   777.9 ns |   771.7 ns |  42.30 ns |  0.72 |    0.08 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,681.6 ns |   181.8 ns |   9.96 ns |  1.55 |    0.16 |    3 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   953.9 ns |   847.5 ns |  46.46 ns |  0.88 |    0.10 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   989.2 ns | 1,022.4 ns |  56.04 ns |  0.91 |    0.11 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
