```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,857.5 ns |   795.5 ns |  43.60 ns |  1.00 |    0.01 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,471.6 ns |   311.2 ns |  17.06 ns |  0.64 |    0.01 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1,119.7 ns |   999.2 ns |  54.77 ns |  0.29 |    0.01 |    1 | 0.0038 | 0.0029 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; | 1,072.1 ns | 1,328.1 ns |  72.80 ns |  0.28 |    0.02 |    1 | 0.0038 | 0.0029 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   909.5 ns | 2,377.9 ns | 130.34 ns |  0.24 |    0.03 |    1 | 0.0038 | 0.0019 |     360 B |        0.20 |
