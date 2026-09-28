```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1.308 μs | 0.1873 μs | 0.0103 μs |  1.00 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1.325 μs | 0.1058 μs | 0.0058 μs |  1.01 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4.929 μs | 0.9842 μs | 0.0539 μs |  3.77 |    0.04 |    4 | 0.0687 | 0.0305 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3.365 μs | 1.4715 μs | 0.0807 μs |  2.57 |    0.06 |    3 | 0.0420 | 0.0381 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1.412 μs | 0.3969 μs | 0.0218 μs |  1.08 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1.454 μs | 0.9067 μs | 0.0497 μs |  1.11 |    0.03 |    1 | 0.0153 | 0.0134 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1.271 μs | 0.4109 μs | 0.0225 μs |  0.97 |    0.02 |    1 | 0.0134 | 0.0114 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1.219 μs | 0.1562 μs | 0.0086 μs |  0.93 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1.192 μs | 0.1685 μs | 0.0092 μs |  0.91 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          | 1.007 μs | 0.5434 μs | 0.0298 μs |  0.77 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1.992 μs | 0.2999 μs | 0.0164 μs |  1.52 |    0.02 |    2 | 0.0191 | 0.0153 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.179 μs | 0.1346 μs | 0.0074 μs |  0.90 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.139 μs | 0.0411 μs | 0.0023 μs |  0.87 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.20 |
