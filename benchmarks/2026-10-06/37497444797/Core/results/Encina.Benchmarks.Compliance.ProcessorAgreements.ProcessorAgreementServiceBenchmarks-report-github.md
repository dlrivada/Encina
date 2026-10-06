```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1.670 μs | 0.2746 μs | 0.0151 μs |  1.00 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1.766 μs | 1.4976 μs | 0.0821 μs |  1.06 |    0.04 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5.743 μs | 1.2354 μs | 0.0677 μs |  3.44 |    0.04 |    4 | 0.0992 | 0.0458 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3.792 μs | 0.4283 μs | 0.0235 μs |  2.27 |    0.02 |    3 | 0.0648 | 0.0305 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1.820 μs | 0.3900 μs | 0.0214 μs |  1.09 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1.923 μs | 0.6890 μs | 0.0378 μs |  1.15 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1.597 μs | 0.1966 μs | 0.0108 μs |  0.96 |    0.01 |    1 | 0.0210 | 0.0191 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1.639 μs | 1.7239 μs | 0.0945 μs |  0.98 |    0.05 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1.634 μs | 0.4815 μs | 0.0264 μs |  0.98 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          | 1.236 μs | 0.3580 μs | 0.0196 μs |  0.74 |    0.01 |    1 | 0.0229 | 0.0210 |     384 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 2.684 μs | 0.8304 μs | 0.0455 μs |  1.61 |    0.03 |    2 | 0.0305 | 0.0267 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.557 μs | 1.7669 μs | 0.0969 μs |  0.93 |    0.05 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.476 μs | 1.7836 μs | 0.0978 μs |  0.88 |    0.05 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
