```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,012.3 ns | 1,169.4 ns | 64.10 ns |  1.00 |    0.08 |    2 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,176.7 ns | 1,380.9 ns | 75.69 ns |  1.17 |    0.09 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,901.6 ns |   395.3 ns | 21.67 ns |  3.86 |    0.21 |    5 | 0.0191 | 0.0153 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,462.2 ns | 1,669.1 ns | 91.49 ns |  2.44 |    0.15 |    4 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,016.4 ns | 1,474.7 ns | 80.83 ns |  1.01 |    0.09 |    2 | 0.0038 | 0.0029 |     384 B |        1.09 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,058.9 ns |   873.8 ns | 47.89 ns |  1.05 |    0.07 |    2 | 0.0038 | 0.0019 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,077.6 ns | 1,035.3 ns | 56.75 ns |  1.07 |    0.07 |    2 | 0.0038 | 0.0029 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,015.1 ns |   666.8 ns | 36.55 ns |  1.01 |    0.06 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,001.2 ns |   578.5 ns | 31.71 ns |  0.99 |    0.06 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   767.8 ns |   559.8 ns | 30.68 ns |  0.76 |    0.05 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,531.7 ns |   285.2 ns | 15.63 ns |  1.52 |    0.08 |    3 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,019.9 ns |   166.1 ns |  9.11 ns |  1.01 |    0.05 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   972.1 ns | 1,146.6 ns | 62.85 ns |  0.96 |    0.07 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
