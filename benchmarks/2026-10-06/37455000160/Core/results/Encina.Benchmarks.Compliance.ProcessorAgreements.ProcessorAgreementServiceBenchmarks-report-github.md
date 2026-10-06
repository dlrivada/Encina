```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,047.0 ns |   619.93 ns |  33.98 ns |  1.00 |    0.04 |    1 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,119.3 ns |   579.67 ns |  31.77 ns |  1.07 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,607.7 ns |   311.61 ns |  17.08 ns |  3.45 |    0.10 |    4 | 0.0191 | 0.0153 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 2,373.6 ns | 1,888.56 ns | 103.52 ns |  2.27 |    0.11 |    3 | 0.0114 | 0.0076 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   934.1 ns | 2,234.97 ns | 122.51 ns |  0.89 |    0.10 |    1 | 0.0038 | 0.0029 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,002.2 ns |   941.85 ns |  51.63 ns |  0.96 |    0.05 |    1 | 0.0038 | 0.0029 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   991.8 ns |   490.46 ns |  26.88 ns |  0.95 |    0.03 |    1 | 0.0038 | 0.0029 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,006.8 ns | 1,191.76 ns |  65.32 ns |  0.96 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,002.2 ns |   237.28 ns |  13.01 ns |  0.96 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   768.8 ns |    85.37 ns |   4.68 ns |  0.73 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,505.4 ns | 1,764.79 ns |  96.73 ns |  1.44 |    0.09 |    2 | 0.0057 | 0.0038 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   979.0 ns |   124.36 ns |   6.82 ns |  0.94 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   971.3 ns |   343.99 ns |  18.86 ns |  0.93 |    0.03 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
