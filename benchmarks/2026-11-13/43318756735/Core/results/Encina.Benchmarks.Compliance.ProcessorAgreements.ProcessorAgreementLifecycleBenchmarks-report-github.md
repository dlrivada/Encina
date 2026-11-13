```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 5.386 μs | 2.2144 μs | 0.1214 μs |  1.00 |    0.03 |    3 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 3.437 μs | 1.9600 μs | 0.1074 μs |  0.64 |    0.02 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1.234 μs | 0.4219 μs | 0.0231 μs |  0.23 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1.291 μs | 0.7148 μs | 0.0392 μs |  0.24 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; | 1.130 μs | 0.5053 μs | 0.0277 μs |  0.21 |    0.01 |    1 | 0.0210 | 0.0191 |     360 B |        0.20 |
