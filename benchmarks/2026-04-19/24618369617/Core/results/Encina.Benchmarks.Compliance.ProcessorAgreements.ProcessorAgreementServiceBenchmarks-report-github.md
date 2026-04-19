```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,186.3 ns | 20.17 ns | 28.28 ns |  1.00 |    0.03 |    4 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,212.7 ns | 19.82 ns | 27.12 ns |  1.02 |    0.03 |    4 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,140.9 ns | 33.87 ns | 49.65 ns |  4.34 |    0.11 |    8 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,204.6 ns | 42.68 ns | 62.56 ns |  2.70 |    0.08 |    7 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,277.7 ns | 20.81 ns | 30.50 ns |  1.08 |    0.04 |    5 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,308.9 ns | 17.85 ns | 25.60 ns |  1.10 |    0.03 |    5 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,171.8 ns | 32.73 ns | 48.99 ns |  0.99 |    0.05 |    4 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,084.3 ns | 20.52 ns | 30.08 ns |  0.91 |    0.03 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,089.6 ns | 28.14 ns | 41.25 ns |  0.92 |    0.04 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   881.3 ns | 17.76 ns | 26.03 ns |  0.74 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,095.2 ns | 36.71 ns | 51.47 ns |  1.77 |    0.06 |    6 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,027.5 ns | 12.89 ns | 18.89 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   996.1 ns | 16.33 ns | 23.42 ns |  0.84 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
