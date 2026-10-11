```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,209.3 ns | 14.65 ns | 21.93 ns |  1.00 |    0.03 |    2 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,256.7 ns | 14.49 ns | 20.32 ns |  1.04 |    0.02 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;ExecuteDPA (new agreement)&#39;               | 5,022.3 ns | 25.17 ns | 37.67 ns |  4.15 |    0.08 |    5 | 0.0992 | 0.0458 |    1776 B |        4.62 |
| &#39;AmendDPA (update terms)&#39;                  | 3,201.2 ns | 54.81 ns | 80.35 ns |  2.65 |    0.08 |    4 | 0.0648 | 0.0305 |    1168 B |        3.04 |
| &#39;AuditDPA (record audit)&#39;                  | 1,315.1 ns | 22.49 ns | 33.66 ns |  1.09 |    0.03 |    2 | 0.0210 | 0.0191 |     400 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,333.8 ns | 23.19 ns | 34.70 ns |  1.10 |    0.03 |    2 | 0.0229 | 0.0210 |     424 B |        1.10 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,176.3 ns | 29.65 ns | 42.53 ns |  0.97 |    0.04 |    2 | 0.0210 | 0.0191 |     392 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,164.4 ns | 16.37 ns | 24.50 ns |  0.96 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,126.3 ns | 16.90 ns | 24.23 ns |  0.93 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   930.7 ns | 31.71 ns | 47.46 ns |  0.77 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
| &#39;RegisterProcessor (new processor)&#39;        | 2,151.6 ns | 24.06 ns | 36.01 ns |  1.78 |    0.04 |    3 | 0.0305 | 0.0267 |     632 B |        1.65 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,165.4 ns | 17.62 ns | 26.37 ns |  0.96 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,125.5 ns | 19.14 ns | 28.65 ns |  0.93 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
