```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   643.3 ns |  28.01 ns |  1.54 ns |  1.00 |    0.00 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   640.5 ns |  40.09 ns |  2.20 ns |  1.00 |    0.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 2,938.4 ns |  73.49 ns |  4.03 ns |  4.57 |    0.01 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 1,765.6 ns | 814.75 ns | 44.66 ns |  2.74 |    0.06 |    3 | 0.0648 | 0.0324 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   661.1 ns |  81.89 ns |  4.49 ns |  1.03 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   715.5 ns | 750.38 ns | 41.13 ns |  1.11 |    0.06 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   597.8 ns |  43.27 ns |  2.37 ns |  0.93 |    0.00 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   571.1 ns | 122.00 ns |  6.69 ns |  0.89 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   543.1 ns |  51.77 ns |  2.84 ns |  0.84 |    0.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   459.7 ns | 239.10 ns | 13.11 ns |  0.71 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,101.4 ns | 128.57 ns |  7.05 ns |  1.71 |    0.01 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   544.0 ns |  22.98 ns |  1.26 ns |  0.85 |    0.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   533.8 ns |  32.67 ns |  1.79 ns |  0.83 |    0.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
