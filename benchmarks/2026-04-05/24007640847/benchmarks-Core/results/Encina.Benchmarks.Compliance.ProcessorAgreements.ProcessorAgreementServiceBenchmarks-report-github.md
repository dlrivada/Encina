```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                     | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------- |----------:|------:|------:|-----:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |  8.517 ms |    NA |  1.00 |    1 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |  8.609 ms |    NA |  1.01 |    3 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               |  9.614 ms |    NA |  1.13 |   12 |    4304 B |       12.23 |
| &#39;AmendDPA (update terms)&#39;                  |  9.397 ms |    NA |  1.10 |   11 |    2064 B |        5.86 |
| &#39;AuditDPA (record audit)&#39;                  |  9.135 ms |    NA |  1.07 |    8 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             |  9.282 ms |    NA |  1.09 |   10 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |  9.044 ms |    NA |  1.06 |    7 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |  8.719 ms |    NA |  1.02 |    4 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |  8.822 ms |    NA |  1.04 |    6 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          | 20.084 ms |    NA |  2.36 |   13 |     384 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        |  9.143 ms |    NA |  1.07 |    9 |     736 B |        2.09 |
| &#39;GetProcessor by ID (cached read)&#39;         |  8.723 ms |    NA |  1.02 |    5 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |  8.578 ms |    NA |  1.01 |    2 |     424 B |        1.20 |
