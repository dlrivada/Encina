```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,903.7 ns |   879.3 ns |  48.20 ns |  1.00 |    0.02 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,415.0 ns |   594.1 ns |  32.57 ns |  0.62 |    0.01 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   998.3 ns |   575.5 ns |  31.54 ns |  0.26 |    0.01 |    1 | 0.0038 | 0.0029 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; | 1,014.9 ns |   944.0 ns |  51.75 ns |  0.26 |    0.01 |    1 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   911.3 ns | 1,949.6 ns | 106.86 ns |  0.23 |    0.02 |    1 | 0.0038 | 0.0029 |     376 B |        0.21 |
