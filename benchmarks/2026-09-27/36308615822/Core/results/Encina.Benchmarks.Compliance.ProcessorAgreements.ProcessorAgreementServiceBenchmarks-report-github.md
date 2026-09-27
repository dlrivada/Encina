```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,052.3 ns |   655.94 ns |  35.95 ns |  1.00 |    0.04 |    1 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   904.9 ns | 1,411.37 ns |  77.36 ns |  0.86 |    0.07 |    1 | 0.0038 | 0.0019 |     424 B |        1.15 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,780.5 ns |   404.86 ns |  22.19 ns |  3.60 |    0.11 |    4 | 0.0191 | 0.0153 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,433.4 ns |   744.41 ns |  40.80 ns |  2.31 |    0.08 |    3 | 0.0114 | 0.0076 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   957.3 ns | 2,229.34 ns | 122.20 ns |  0.91 |    0.10 |    1 | 0.0038 | 0.0029 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   979.0 ns | 2,266.54 ns | 124.24 ns |  0.93 |    0.11 |    1 | 0.0038 | 0.0029 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   999.5 ns |   698.26 ns |  38.27 ns |  0.95 |    0.04 |    1 | 0.0038 | 0.0029 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,070.4 ns |    66.87 ns |   3.67 ns |  1.02 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,006.9 ns |   378.47 ns |  20.75 ns |  0.96 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   804.1 ns |   461.82 ns |  25.31 ns |  0.76 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,458.4 ns | 1,842.65 ns | 101.00 ns |  1.39 |    0.09 |    2 | 0.0057 | 0.0038 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   967.0 ns |   719.79 ns |  39.45 ns |  0.92 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   977.9 ns | 1,064.02 ns |  58.32 ns |  0.93 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
