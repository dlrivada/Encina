```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   997.9 ns |   559.6 ns |  30.67 ns |  1.00 |    0.04 |    2 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,020.9 ns |   988.0 ns |  54.16 ns |  1.02 |    0.05 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,388.1 ns | 1,575.7 ns |  86.37 ns |  3.40 |    0.12 |    5 | 0.0191 | 0.0153 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,166.0 ns |   242.8 ns |  13.31 ns |  2.17 |    0.06 |    4 | 0.0114 | 0.0076 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   921.5 ns | 1,562.0 ns |  85.62 ns |  0.92 |    0.08 |    2 | 0.0038 | 0.0029 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   916.4 ns | 2,326.2 ns | 127.51 ns |  0.92 |    0.11 |    2 | 0.0038 | 0.0029 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   912.8 ns | 1,415.4 ns |  77.58 ns |  0.92 |    0.07 |    2 | 0.0038 | 0.0029 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   953.7 ns | 1,342.4 ns |  73.58 ns |  0.96 |    0.07 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   908.9 ns |   220.2 ns |  12.07 ns |  0.91 |    0.03 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   708.3 ns |   613.0 ns |  33.60 ns |  0.71 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,317.1 ns |   347.2 ns |  19.03 ns |  1.32 |    0.04 |    3 | 0.0057 | 0.0038 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   890.8 ns |   632.8 ns |  34.69 ns |  0.89 |    0.04 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   844.3 ns |   944.8 ns |  51.79 ns |  0.85 |    0.05 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
