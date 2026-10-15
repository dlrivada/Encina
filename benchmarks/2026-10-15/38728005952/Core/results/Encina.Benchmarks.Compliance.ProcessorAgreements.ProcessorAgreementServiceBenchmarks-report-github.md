```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   951.1 ns | 1,383.9 ns |  75.86 ns |  1.00 |    0.10 |    1 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,066.5 ns | 1,123.3 ns |  61.57 ns |  1.13 |    0.09 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,861.9 ns | 1,077.6 ns |  59.07 ns |  4.08 |    0.28 |    4 | 0.0191 | 0.0153 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,417.5 ns |   424.4 ns |  23.26 ns |  2.55 |    0.17 |    3 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,080.4 ns |   698.8 ns |  38.30 ns |  1.14 |    0.08 |    1 | 0.0038 | 0.0029 |     384 B |        1.09 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,091.6 ns |   429.6 ns |  23.55 ns |  1.15 |    0.08 |    1 | 0.0038 | 0.0029 |     408 B |        1.16 |
| &#39;TerminateDPA (end agreement)&#39;             |   866.0 ns | 2,155.9 ns | 118.17 ns |  0.91 |    0.12 |    1 | 0.0038 | 0.0029 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   942.3 ns |   582.8 ns |  31.94 ns |  0.99 |    0.07 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,053.7 ns |   972.1 ns |  53.28 ns |  1.11 |    0.09 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   758.2 ns |   504.3 ns |  27.64 ns |  0.80 |    0.06 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,553.7 ns | 1,245.7 ns |  68.28 ns |  1.64 |    0.13 |    2 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   999.8 ns |   897.8 ns |  49.21 ns |  1.06 |    0.08 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   955.2 ns |   443.4 ns |  24.31 ns |  1.01 |    0.07 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
