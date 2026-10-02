```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   991.9 ns | 1,819.3 ns |  99.72 ns |  1.01 |    0.12 |    1 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,132.4 ns |   726.0 ns |  39.79 ns |  1.15 |    0.10 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,950.3 ns | 2,554.2 ns | 140.00 ns |  4.01 |    0.36 |    4 | 0.0191 | 0.0153 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,543.4 ns |   241.9 ns |  13.26 ns |  2.58 |    0.22 |    3 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  |   919.8 ns | 3,072.3 ns | 168.41 ns |  0.93 |    0.17 |    1 | 0.0038 | 0.0019 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             |   950.7 ns |   988.3 ns |  54.17 ns |  0.96 |    0.10 |    1 | 0.0038 | 0.0029 |     408 B |        1.16 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,023.0 ns | 1,362.4 ns |  74.68 ns |  1.04 |    0.11 |    1 | 0.0038 | 0.0029 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,020.6 ns |   212.7 ns |  11.66 ns |  1.04 |    0.09 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,034.6 ns | 1,259.5 ns |  69.04 ns |  1.05 |    0.11 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   793.6 ns |   892.7 ns |  48.93 ns |  0.81 |    0.08 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,491.5 ns |   689.0 ns |  37.77 ns |  1.51 |    0.13 |    2 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         |   982.4 ns |   424.6 ns |  23.28 ns |  1.00 |    0.09 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   996.0 ns | 1,077.0 ns |  59.03 ns |  1.01 |    0.10 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
