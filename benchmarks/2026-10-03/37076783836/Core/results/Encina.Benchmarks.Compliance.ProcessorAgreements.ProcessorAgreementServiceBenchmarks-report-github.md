```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.75GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,264.1 ns |    29.34 ns |  1.61 ns |  1.00 |    0.00 |    1 | 0.0134 | 0.0114 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,245.7 ns |   103.70 ns |  5.68 ns |  0.99 |    0.00 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,740.2 ns | 1,036.14 ns | 56.79 ns |  3.75 |    0.04 |    4 | 0.0687 | 0.0305 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,128.8 ns |   296.13 ns | 16.23 ns |  2.48 |    0.01 |    3 | 0.0420 | 0.0381 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,363.5 ns |   423.27 ns | 23.20 ns |  1.08 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,397.5 ns |   939.76 ns | 51.51 ns |  1.11 |    0.04 |    1 | 0.0153 | 0.0134 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,225.3 ns |   185.59 ns | 10.17 ns |  0.97 |    0.01 |    1 | 0.0134 | 0.0114 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,135.3 ns |   347.88 ns | 19.07 ns |  0.90 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,112.2 ns |   212.34 ns | 11.64 ns |  0.88 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   970.4 ns |   501.53 ns | 27.49 ns |  0.77 |    0.02 |    1 | 0.0153 | 0.0143 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,961.0 ns |   214.00 ns | 11.73 ns |  1.55 |    0.01 |    2 | 0.0191 | 0.0153 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,110.5 ns |   175.03 ns |  9.59 ns |  0.88 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,114.5 ns |   256.54 ns | 14.06 ns |  0.88 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
