```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,051.1 ns |   748.1 ns |  41.00 ns |  1.00 |    0.05 |    1 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,089.2 ns |   498.9 ns |  27.35 ns |  1.04 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,733.2 ns | 2,266.8 ns | 124.25 ns |  3.56 |    0.16 |    3 | 0.0191 | 0.0153 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,405.2 ns | 1,234.0 ns |  67.64 ns |  2.29 |    0.09 |    2 | 0.0114 | 0.0076 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  | 1,089.5 ns |   704.0 ns |  38.59 ns |  1.04 |    0.05 |    1 | 0.0038 | 0.0029 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,191.8 ns | 1,272.6 ns |  69.76 ns |  1.14 |    0.07 |    1 | 0.0038 | 0.0029 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   888.3 ns |   310.5 ns |  17.02 ns |  0.85 |    0.03 |    1 | 0.0038 | 0.0029 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,020.7 ns | 1,170.9 ns |  64.18 ns |  0.97 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   995.8 ns |   832.5 ns |  45.63 ns |  0.95 |    0.05 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   774.8 ns |   259.5 ns |  14.23 ns |  0.74 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,441.2 ns |   349.6 ns |  19.16 ns |  1.37 |    0.05 |    1 | 0.0057 | 0.0038 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   968.9 ns |   752.9 ns |  41.27 ns |  0.92 |    0.05 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   979.1 ns |   651.6 ns |  35.72 ns |  0.93 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
