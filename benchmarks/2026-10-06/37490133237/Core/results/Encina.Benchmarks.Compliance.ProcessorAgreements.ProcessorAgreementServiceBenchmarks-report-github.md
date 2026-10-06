```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,280.2 ns |   945.1 ns |  51.80 ns |  1.00 |    0.05 |    1 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,104.2 ns |   315.0 ns |  17.27 ns |  0.86 |    0.03 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,695.5 ns | 3,749.4 ns | 205.52 ns |  3.67 |    0.19 |    4 | 0.0153 | 0.0076 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 3,270.2 ns |   495.1 ns |  27.14 ns |  2.56 |    0.09 |    3 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,257.2 ns | 2,183.1 ns | 119.67 ns |  0.98 |    0.09 |    1 | 0.0038 | 0.0019 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,168.3 ns | 1,509.6 ns |  82.74 ns |  0.91 |    0.06 |    1 | 0.0038 | 0.0019 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,169.7 ns | 2,580.1 ns | 141.42 ns |  0.91 |    0.10 |    1 | 0.0038 | 0.0019 |     360 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               | 1,040.3 ns |   826.3 ns |  45.29 ns |  0.81 |    0.04 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   992.2 ns |   640.5 ns |  35.11 ns |  0.78 |    0.04 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   951.6 ns |   970.3 ns |  53.19 ns |  0.74 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,887.2 ns | 1,826.9 ns | 100.14 ns |  1.48 |    0.09 |    2 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,006.6 ns |   229.8 ns |  12.60 ns |  0.79 |    0.03 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   958.8 ns |   297.8 ns |  16.32 ns |  0.75 |    0.03 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
