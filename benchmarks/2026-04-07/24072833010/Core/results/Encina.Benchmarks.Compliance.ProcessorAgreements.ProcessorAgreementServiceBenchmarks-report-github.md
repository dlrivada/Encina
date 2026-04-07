```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                     | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------- |----------:|------:|------:|-----:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |  8.550 ms |    NA |  1.00 |    4 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |  8.540 ms |    NA |  1.00 |    3 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               |  9.397 ms |    NA |  1.10 |   12 |    4304 B |       12.23 |
| &#39;AmendDPA (update terms)&#39;                  |  9.128 ms |    NA |  1.07 |   11 |    2064 B |        5.86 |
| &#39;AuditDPA (record audit)&#39;                  |  8.795 ms |    NA |  1.03 |    7 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             |  8.970 ms |    NA |  1.05 |   10 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |  8.837 ms |    NA |  1.03 |    8 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 10.224 ms |    NA |  1.20 |   13 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |  8.503 ms |    NA |  0.99 |    1 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |  8.530 ms |    NA |  1.00 |    2 |     384 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        |  8.949 ms |    NA |  1.05 |    9 |     736 B |        2.09 |
| &#39;GetProcessor by ID (cached read)&#39;         |  8.650 ms |    NA |  1.01 |    6 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |  8.579 ms |    NA |  1.00 |    5 |     424 B |        1.20 |
