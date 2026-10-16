```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;          |   724.1 ns |   177.02 ns |   9.70 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39;  |   747.1 ns |   575.61 ns |  31.55 ns |  1.03 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;ExecuteDPA (new agreement)&#39;               | 3,278.1 ns |   433.47 ns |  23.76 ns |  4.53 |    0.06 |    4 | 0.1030 | 0.0496 |    1776 B |        4.83 |
| &#39;AmendDPA (update terms)&#39;                  | 1,936.1 ns | 2,817.32 ns | 154.43 ns |  2.67 |    0.19 |    3 | 0.0648 | 0.0305 |    1104 B |        3.00 |
| &#39;AuditDPA (record audit)&#39;                  |   717.0 ns |   138.89 ns |   7.61 ns |  0.99 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        1.04 |
| &#39;RenewDPA (extend expiration)&#39;             |   722.6 ns |   266.82 ns |  14.63 ns |  1.00 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        1.11 |
| &#39;TerminateDPA (end agreement)&#39;             |   672.7 ns |   108.31 ns |   5.94 ns |  0.93 |    0.01 |    1 | 0.0210 | 0.0200 |     376 B |        1.02 |
| &#39;GetDPA by ID (cached read)&#39;               |   613.5 ns |    80.99 ns |   4.44 ns |  0.85 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;             |   599.1 ns |   114.81 ns |   6.29 ns |  0.83 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;          |   494.1 ns |   133.80 ns |   7.33 ns |  0.68 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
| &#39;RegisterProcessor (new processor)&#39;        | 1,195.7 ns |   255.33 ns |  14.00 ns |  1.65 |    0.03 |    2 | 0.0324 | 0.0305 |     568 B |        1.54 |
| &#39;GetProcessor by ID (cached read)&#39;         |   598.6 ns |   413.63 ns |  22.67 ns |  0.83 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   601.1 ns |   484.12 ns |  26.54 ns |  0.83 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
