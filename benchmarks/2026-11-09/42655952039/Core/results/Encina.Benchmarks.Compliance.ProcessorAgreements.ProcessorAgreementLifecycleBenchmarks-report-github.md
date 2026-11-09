```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4.555 μs | 4.1498 μs | 0.2275 μs |  1.00 |    0.06 |    3 | 0.0153 | 0.0076 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.075 μs | 0.4080 μs | 0.0224 μs |  0.68 |    0.03 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.165 μs | 0.9185 μs | 0.0503 μs |  0.26 |    0.01 |    1 | 0.0038 | 0.0019 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.298 μs | 3.7941 μs | 0.2080 μs |  0.29 |    0.04 |    1 | 0.0038 | 0.0019 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.129 μs | 0.3560 μs | 0.0195 μs |  0.25 |    0.01 |    1 | 0.0038 | 0.0019 |     360 B |        0.20 |
