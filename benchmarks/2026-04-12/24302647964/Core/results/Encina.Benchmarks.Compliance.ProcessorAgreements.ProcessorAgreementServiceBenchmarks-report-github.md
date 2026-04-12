```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,192.0 ns | 12.45 ns | 18.24 ns |  1.00 |    0.02 |    4 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,194.2 ns | 14.81 ns | 21.72 ns |  1.00 |    0.02 |    4 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,150.2 ns | 37.48 ns | 51.30 ns |  4.32 |    0.08 |    8 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,209.5 ns | 65.99 ns | 98.77 ns |  2.69 |    0.09 |    7 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,228.1 ns | 14.00 ns | 20.96 ns |  1.03 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,298.3 ns | 11.94 ns | 17.50 ns |  1.09 |    0.02 |    5 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,106.4 ns | 17.23 ns | 25.26 ns |  0.93 |    0.03 |    3 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,067.2 ns | 21.99 ns | 32.23 ns |  0.90 |    0.03 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,008.8 ns | 12.03 ns | 17.26 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   862.8 ns | 19.39 ns | 28.43 ns |  0.72 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,059.6 ns | 24.84 ns | 36.41 ns |  1.73 |    0.04 |    6 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,012.7 ns | 11.92 ns | 17.48 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   984.4 ns | 13.31 ns | 19.51 ns |  0.83 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
