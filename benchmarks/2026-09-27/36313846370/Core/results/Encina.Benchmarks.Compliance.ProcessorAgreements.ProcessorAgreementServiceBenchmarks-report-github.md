```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,256.7 ns | 437.8 ns | 24.00 ns |  1.00 |    0.02 |    1 | 0.0134 | 0.0114 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,286.5 ns | 425.4 ns | 23.32 ns |  1.02 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,784.0 ns | 310.9 ns | 17.04 ns |  3.81 |    0.06 |    4 | 0.0687 | 0.0305 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,154.7 ns | 873.1 ns | 47.85 ns |  2.51 |    0.05 |    3 | 0.0420 | 0.0381 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,366.1 ns | 402.1 ns | 22.04 ns |  1.09 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,409.4 ns | 859.4 ns | 47.11 ns |  1.12 |    0.04 |    1 | 0.0153 | 0.0134 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,232.5 ns | 190.5 ns | 10.44 ns |  0.98 |    0.02 |    1 | 0.0134 | 0.0114 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,140.7 ns | 115.1 ns |  6.31 ns |  0.91 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,137.9 ns | 165.4 ns |  9.06 ns |  0.91 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   957.1 ns | 302.4 ns | 16.57 ns |  0.76 |    0.02 |    1 | 0.0153 | 0.0143 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,943.0 ns | 496.0 ns | 27.19 ns |  1.55 |    0.03 |    2 | 0.0191 | 0.0153 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,107.8 ns | 191.1 ns | 10.47 ns |  0.88 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,115.9 ns | 147.3 ns |  8.08 ns |  0.89 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
