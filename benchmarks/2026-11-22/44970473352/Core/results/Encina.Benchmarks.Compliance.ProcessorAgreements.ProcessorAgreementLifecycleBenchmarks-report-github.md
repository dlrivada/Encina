```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3.465 μs | 0.0265 μs | 0.0388 μs |  1.00 |    0.02 |    3 | 0.0191 | 0.0153 |    1840 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2.141 μs | 0.0370 μs | 0.0553 μs |  0.62 |    0.02 |    2 | 0.0114 | 0.0076 |    1168 B |        0.63 |
| &#39;AuditDPA (record audit)&#39;      | 1.132 μs | 0.1409 μs | 0.2108 μs |  0.33 |    0.06 |    1 | 0.0038 | 0.0029 |     384 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.292 μs | 0.1672 μs | 0.2503 μs |  0.37 |    0.07 |    1 | 0.0038 | 0.0029 |     408 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.177 μs | 0.0837 μs | 0.1253 μs |  0.34 |    0.04 |    1 | 0.0038 | 0.0029 |     376 B |        0.20 |
