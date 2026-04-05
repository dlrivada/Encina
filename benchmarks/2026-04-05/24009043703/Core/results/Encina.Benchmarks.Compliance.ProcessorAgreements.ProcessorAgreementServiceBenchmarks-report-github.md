```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                     | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|------:|------:|-----:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 8.477 ms |    NA |  1.00 |    1 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 8.564 ms |    NA |  1.01 |    4 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 9.327 ms |    NA |  1.10 |   12 |    4304 B |       12.23 |
| &#39;AmendDPA (update terms)&#39;                  | 9.105 ms |    NA |  1.07 |   11 |    2064 B |        5.86 |
| &#39;AuditDPA (record audit)&#39;                  | 8.943 ms |    NA |  1.05 |   10 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 9.372 ms |    NA |  1.11 |   13 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 8.699 ms |    NA |  1.03 |    8 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 8.574 ms |    NA |  1.01 |    5 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 8.613 ms |    NA |  1.02 |    7 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          | 8.504 ms |    NA |  1.00 |    2 |     384 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 8.917 ms |    NA |  1.05 |    9 |     736 B |        2.09 |
| &#39;GetProcessor by ID (cached read)&#39;         | 8.575 ms |    NA |  1.01 |    6 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 8.534 ms |    NA |  1.01 |    3 |     424 B |        1.20 |
