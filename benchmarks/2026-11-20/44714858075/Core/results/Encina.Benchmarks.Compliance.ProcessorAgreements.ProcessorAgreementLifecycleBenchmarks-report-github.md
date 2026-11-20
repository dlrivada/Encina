```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4.575 μs | 4.0953 μs | 0.2245 μs |  1.00 |    0.06 |    3 | 0.0153 | 0.0076 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.086 μs | 0.7662 μs | 0.0420 μs |  0.68 |    0.03 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.200 μs | 1.1534 μs | 0.0632 μs |  0.26 |    0.02 |    1 | 0.0038 | 0.0019 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.195 μs | 1.1502 μs | 0.0630 μs |  0.26 |    0.02 |    1 | 0.0038 | 0.0019 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.171 μs | 3.4430 μs | 0.1887 μs |  0.26 |    0.04 |    1 | 0.0038 | 0.0019 |     360 B |        0.20 |
