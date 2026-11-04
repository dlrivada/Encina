```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.47GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4.943 μs | 1.0119 μs | 0.0555 μs |  1.00 |    3 | 0.0687 | 0.0305 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.336 μs | 1.0049 μs | 0.0551 μs |  0.68 |    2 | 0.0420 | 0.0381 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.423 μs | 0.2611 μs | 0.0143 μs |  0.29 |    1 | 0.0134 | 0.0114 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.432 μs | 0.3567 μs | 0.0196 μs |  0.29 |    1 | 0.0153 | 0.0134 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.285 μs | 0.3985 μs | 0.0218 μs |  0.26 |    1 | 0.0134 | 0.0114 |     360 B |        0.20 |
