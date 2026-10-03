```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          | 1,127.1 ns | 1,029.5 ns |  56.43 ns |  1.00 |    0.06 |    1 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  | 1,048.0 ns | 1,235.7 ns |  67.74 ns |  0.93 |    0.07 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 4,357.2 ns | 2,476.3 ns | 135.74 ns |  3.87 |    0.20 |    4 | 0.0153 | 0.0076 |    1776 B |        5.05 |
| &#39;AmendDPA (update terms)&#39;                  | 2,837.3 ns |   429.5 ns |  23.54 ns |  2.52 |    0.11 |    3 | 0.0114 | 0.0076 |    1104 B |        3.14 |
| &#39;AuditDPA (record audit)&#39;                  | 1,091.6 ns |   921.6 ns |  50.52 ns |  0.97 |    0.06 |    1 | 0.0038 | 0.0019 |     368 B |        1.05 |
| &#39;RenewDPA (extend expiration)&#39;             | 1,223.8 ns | 3,665.1 ns | 200.90 ns |  1.09 |    0.16 |    1 | 0.0038 | 0.0019 |     392 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             | 1,240.9 ns |   675.5 ns |  37.03 ns |  1.10 |    0.06 |    1 | 0.0038 | 0.0029 |     376 B |        1.07 |
| &#39;GetDPA by ID (cached read)&#39;               |   917.1 ns |   965.6 ns |  52.93 ns |  0.82 |    0.05 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             | 1,154.5 ns |   771.4 ns |  42.28 ns |  1.03 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   887.7 ns |   724.9 ns |  39.73 ns |  0.79 |    0.05 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,743.7 ns |   870.5 ns |  47.71 ns |  1.55 |    0.08 |    2 | 0.0057 | 0.0038 |     568 B |        1.61 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,160.9 ns |   851.3 ns |  46.66 ns |  1.03 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1,138.6 ns |   930.3 ns |  50.99 ns |  1.01 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
