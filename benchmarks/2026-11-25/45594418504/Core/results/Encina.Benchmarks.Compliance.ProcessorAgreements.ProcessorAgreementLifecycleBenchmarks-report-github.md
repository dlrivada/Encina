```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 5.490 μs | 0.6284 μs | 0.0344 μs |  1.00 |    3 | 0.0687 | 0.0305 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.665 μs | 0.3792 μs | 0.0208 μs |  0.67 |    2 | 0.0420 | 0.0381 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.649 μs | 0.4862 μs | 0.0266 μs |  0.30 |    1 | 0.0134 | 0.0114 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.743 μs | 0.7015 μs | 0.0385 μs |  0.32 |    1 | 0.0153 | 0.0134 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.487 μs | 0.9236 μs | 0.0506 μs |  0.27 |    1 | 0.0134 | 0.0114 |     360 B |        0.20 |
